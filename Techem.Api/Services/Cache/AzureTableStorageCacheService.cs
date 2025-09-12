using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

public class AzureTableStorageCacheService : ICacheService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableStorageCacheService> _logger;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(1);
    private readonly Task _tableInitializationTask;

    public AzureTableStorageCacheService(IConfiguration configuration, ILogger<AzureTableStorageCacheService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration.GetConnectionString("AzureStorage");
        var tableName = configuration["AzureStorage:TableName"] ?? "deviceconfigurations";
        var storageAccountName = configuration["AzureStorage:AccountName"];
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Use connection string authentication
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient(tableName);
        }
        else
        {
            // Use managed identity authentication
            if (string.IsNullOrEmpty(storageAccountName))
                throw new InvalidOperationException("Either AzureStorage connection string or AccountName for managed identity must be configured");
                
            var credential = new DefaultAzureCredential();
            var serviceUri = new Uri($"https://{storageAccountName}.table.core.windows.net");
            var serviceClient = new TableServiceClient(serviceUri, credential);
            _tableClient = serviceClient.GetTableClient(tableName);
        }
        
        // Initialize table and track the task
        _tableInitializationTask = InitializeTableAsync(tableName);
    }

    private async Task InitializeTableAsync(string tableName)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 1000;

        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                await _tableClient.CreateIfNotExistsAsync();
                _logger.LogInformation("Azure Table '{TableName}' initialized successfully", tableName);
                return;
            }
            catch (Exception ex) when (retry < maxRetries - 1)
            {
                var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, retry));
                _logger.LogWarning(ex, "Failed to initialize Azure Table '{TableName}', retry {Retry}/{MaxRetries} in {Delay}ms", 
                    tableName, retry + 1, maxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Table '{TableName}' after {MaxRetries} attempts", tableName, maxRetries);
                throw;
            }
        }
    }

    private async Task EnsureTableInitializedAsync()
    {
        try
        {
            await _tableInitializationTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table initialization failed, operations may not work correctly");
            throw;
        }
    }

    public async Task<DeviceConfiguration?> GetConfigurationAsync(string prdv)
    {
        await EnsureTableInitializedAsync();
        
        try
        {
            var response = await _tableClient.GetEntityAsync<DeviceConfigurationEntity>("config", prdv);
            var entity = response.Value;
            
            // Check if entity has expired
            if (entity.ExpiresAt.HasValue && entity.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                _logger.LogDebug("Cache entry expired for PRDV: {Prdv}", prdv);
                // Optionally delete expired entry
                _ = Task.Run(async () => await DeleteEntitySafely("config", prdv));
                return null;
            }
            
            _logger.LogDebug("Cache hit for PRDV: {Prdv}", prdv);
            return entity.ToDeviceConfiguration();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Cache miss for PRDV: {Prdv}", prdv);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration from Azure Table for PRDV: {Prdv}", prdv);
            return null;
        }
    }

    public async Task SetConfigurationAsync(string prdv, DeviceConfiguration configuration)
    {
        await EnsureTableInitializedAsync();
        
        try
        {
            var entity = DeviceConfigurationEntity.FromDeviceConfiguration(configuration, prdv);
            entity.ExpiresAt = DateTimeOffset.UtcNow.Add(_defaultTtl);
            
            var response = await _tableClient.UpsertEntityAsync(entity);
            _logger.LogDebug("Configuration cached in Azure Table for PRDV: {Prdv}", prdv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration in Azure Table for PRDV: {Prdv}", prdv);
        }
    }

    public async Task<bool> ExistsAsync(string prdv)
    {
        await EnsureTableInitializedAsync();
        
        try
        {
            var response = await _tableClient.GetEntityAsync<DeviceConfigurationEntity>("config", prdv);
            var entity = response.Value;
            
            // Check if entity has expired
            if (entity.ExpiresAt.HasValue && entity.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                _logger.LogDebug("Cache entry expired for PRDV: {Prdv}", prdv);
                return false;
            }
            
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence in Azure Table for PRDV: {Prdv}", prdv);
            return false;
        }
    }

    private async Task DeleteEntitySafely(string partitionKey, string rowKey)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete expired entity: PartitionKey={PartitionKey}, RowKey={RowKey}", partitionKey, rowKey);
        }
    }
}

/// <summary>
/// Azure Table entity representation of DeviceConfiguration
/// </summary>
public class DeviceConfigurationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    
    // Custom properties
    public string? StorageInterval { get; set; }
    public bool IsStorageEnabled { get; set; }
    public int MaxDataAgeInDays { get; set; }
    public string? DeviceType { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? AdditionalPropertiesJson { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public static DeviceConfigurationEntity FromDeviceConfiguration(DeviceConfiguration config, string prdv)
    {
        return new DeviceConfigurationEntity
        {
            PartitionKey = "config",
            RowKey = prdv,
            StorageInterval = config.StorageInterval.ToString(),
            IsStorageEnabled = config.IsStorageEnabled,
            MaxDataAgeInDays = config.MaxDataAgeInDays,
            DeviceType = config.DeviceType,
            LastUpdated = config.LastUpdated,
            AdditionalPropertiesJson = config.AdditionalProperties.Any() 
                ? JsonSerializer.Serialize(config.AdditionalProperties) 
                : null
        };
    }

    public DeviceConfiguration ToDeviceConfiguration()
    {
        var config = new DeviceConfiguration
        {
            PrDv = RowKey,
            IsStorageEnabled = IsStorageEnabled,
            MaxDataAgeInDays = MaxDataAgeInDays,
            DeviceType = DeviceType ?? string.Empty,
            LastUpdated = LastUpdated
        };

        // Parse StorageInterval enum
        if (Enum.TryParse<StorageInterval>(StorageInterval, out var interval))
        {
            config.StorageInterval = interval;
        }

        // Parse additional properties
        if (!string.IsNullOrEmpty(AdditionalPropertiesJson))
        {
            try
            {
                var additionalProps = JsonSerializer.Deserialize<Dictionary<string, string>>(AdditionalPropertiesJson);
                if (additionalProps != null)
                {
                    config.AdditionalProperties = additionalProps;
                }
            }
            catch (JsonException ex)
            {
                // Log warning but don't fail - just use empty dictionary
                // Logger is not available in this context, so we'll handle it gracefully
            }
        }

        return config;
    }
}
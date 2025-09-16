using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

/// <summary>
/// Azure Table entity representation of DeviceConfiguration
/// </summary>
public class DeviceConfigurationEntity : ITableEntity
{
    // Optimized JSON serializer options for better performance
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = null, // Use exact property names
        WriteIndented = false // Minimize size for better performance
    };

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
                ? JsonSerializer.Serialize(config.AdditionalProperties, JsonOptions) 
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
                var additionalProps = JsonSerializer.Deserialize<Dictionary<string, string>>(AdditionalPropertiesJson, JsonOptions);
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
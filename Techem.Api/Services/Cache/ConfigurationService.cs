using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

/// <summary>
/// Configuration service that implements cache-aside pattern for device configurations
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ICacheService _cacheService;
    private readonly IConfigurationDatabaseService _databaseService;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly bool _enableDetailedLogging;

    public ConfigurationService(
        ICacheService cacheService,
        IConfigurationDatabaseService databaseService,
        ILogger<ConfigurationService> logger,
        IConfiguration configuration)
    {
        _cacheService = cacheService;
        _databaseService = databaseService;
        _logger = logger;
        _enableDetailedLogging = configuration.GetValue<bool>("LoadTestLogging:EnableDetailedLogging", false);
    }

    public async Task<DeviceConfiguration?> GetConfigurationAsync(string prdv)
    {
        if (string.IsNullOrEmpty(prdv))
        {
            _logger.LogWarning("GetConfigurationAsync called with empty PRDV");
            return null;
        }

        try
        {
            // Step 1: Check cache first (cache-aside pattern)
            _logger.LogDebug("Step 1: Checking cache for PRDV: {Prdv}", prdv);
            var cachedConfig = await _cacheService.GetConfigurationAsync(prdv);
            
            if (cachedConfig != null)
            {
                _logger.LogDebug("Cache hit for PRDV: {Prdv}", prdv);
                return cachedConfig;
            }

            _logger.LogDebug("Cache miss for PRDV: {Prdv}", prdv);

            // Step 2: Cache miss - query database
            _logger.LogDebug("Step 2: Querying database for PRDV: {Prdv}", prdv);
            var dbConfig = await _databaseService.GetConfigurationAsync(prdv);
            
            if (dbConfig == null)
            {
                _logger.LogDebug("Configuration not found in database for PRDV: {Prdv}", prdv);
                return null;
            }

            // Step 3: Cache the result from database
            _logger.LogDebug("Step 3: Caching database result for PRDV: {Prdv}", prdv);
            try
            {
                await _cacheService.SetConfigurationAsync(prdv, dbConfig);
                _logger.LogDebug("Successfully cached configuration for PRDV: {Prdv}", prdv);
            }
            catch (Exception cacheEx)
            {
                // Cache write failures should not prevent returning the data
                _logger.LogWarning(cacheEx, "Failed to cache configuration for PRDV: {Prdv}, but returning database result", prdv);
            }

            // Step 4: Return the result
            if (_enableDetailedLogging)
            {
                _logger.LogInformation("Retrieved configuration for PRDV: {Prdv} from database (DeviceType: {DeviceType})", 
                    prdv, dbConfig.DeviceType);
            }
            return dbConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetConfigurationAsync for PRDV: {Prdv}", prdv);
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string prdv)
    {
        if (string.IsNullOrEmpty(prdv))
        {
            return false;
        }

        try
        {
            // Check cache first for existence
            var existsInCache = await _cacheService.ExistsAsync(prdv);
            if (existsInCache)
            {
                _logger.LogDebug("Configuration exists in cache for PRDV: {Prdv}", prdv);
                return true;
            }

            // If not in cache, check if we can get it from database
            var config = await _databaseService.GetConfigurationAsync(prdv);
            var exists = config != null;
            
            if (exists)
            {
                _logger.LogDebug("Configuration exists in database for PRDV: {Prdv}", prdv);
                // Optionally cache it since we just loaded it
                try
                {
                    await _cacheService.SetConfigurationAsync(prdv, config);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache configuration during existence check for PRDV: {Prdv}", prdv);
                }
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence for PRDV: {Prdv}", prdv);
            return false;
        }
    }

    public async Task SetConfigurationAsync(string prdv, DeviceConfiguration configuration)
    {
        if (string.IsNullOrEmpty(prdv) || configuration == null)
        {
            _logger.LogWarning("SetConfigurationAsync called with invalid parameters");
            return;
        }

        try
        {
            // Update the PRDV in the configuration to ensure consistency
            configuration.PrDv = prdv;
            configuration.LastUpdated = DateTime.UtcNow;

            // Cache the updated configuration
            await _cacheService.SetConfigurationAsync(prdv, configuration);
            if (_enableDetailedLogging)
            {
                _logger.LogInformation("Successfully updated and cached configuration for PRDV: {Prdv}", prdv);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration for PRDV: {Prdv}", prdv);
            throw;
        }
    }

    public async Task<int> SetConfigurationsBatchAsync(Dictionary<string, DeviceConfiguration> configurations)
    {
        if (configurations == null || !configurations.Any())
        {
            _logger.LogWarning("SetConfigurationsBatchAsync called with empty or null configurations");
            return 0;
        }

        try
        {
            // Ensure consistency for all configurations
            var now = DateTime.UtcNow;
            foreach (var kvp in configurations)
            {
                kvp.Value.PrDv = kvp.Key;
                kvp.Value.LastUpdated = now;
            }

            // Use the underlying cache service batch operation
            var successCount = await _cacheService.SetConfigurationsBatchAsync(configurations);
            
            if (_enableDetailedLogging)
            {
                _logger.LogInformation("Successfully processed batch: {SuccessCount}/{TotalCount} configurations cached", 
                    successCount, configurations.Count);
            }
            
            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configurations in batch operation");
            throw;
        }
    }
}
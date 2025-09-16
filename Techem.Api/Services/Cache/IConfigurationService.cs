using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

/// <summary>
/// Configuration service that implements cache-aside pattern for device configurations
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets device configuration using cache-aside pattern:
    /// 1. Check cache first
    /// 2. On cache miss, query database
    /// 3. Cache the result from database
    /// 4. Return the result
    /// </summary>
    /// <param name="prdv">The PRDV identifier of the device</param>
    /// <returns>Device configuration if found, null otherwise</returns>
    Task<DeviceConfiguration?> GetConfigurationAsync(string prdv);
    
    /// <summary>
    /// Checks if a configuration exists (cache-first approach)
    /// </summary>
    /// <param name="prdv">The PRDV identifier of the device</param>
    /// <returns>True if configuration exists, false otherwise</returns>
    Task<bool> ExistsAsync(string prdv);
    
    /// <summary>
    /// Updates a device configuration and refreshes the cache
    /// </summary>
    /// <param name="prdv">The PRDV identifier of the device</param>
    /// <param name="configuration">The updated configuration</param>
    Task SetConfigurationAsync(string prdv, DeviceConfiguration configuration);
    
    /// <summary>
    /// Updates multiple device configurations in a batch operation for improved performance
    /// </summary>
    /// <param name="configurations">Dictionary of PRDV to DeviceConfiguration mappings</param>
    /// <returns>Number of successfully processed configurations</returns>
    Task<int> SetConfigurationsBatchAsync(Dictionary<string, DeviceConfiguration> configurations);
}
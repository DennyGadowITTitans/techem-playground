using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

public interface ICacheService
{
    Task<DeviceConfiguration?> GetConfigurationAsync(string prdv);
    Task SetConfigurationAsync(string prdv, DeviceConfiguration configuration);
    Task<bool> ExistsAsync(string prdv);
    
    /// <summary>
    /// Sets multiple configurations in a batch operation for improved performance
    /// </summary>
    /// <param name="configurations">Dictionary of PRDV to DeviceConfiguration mappings</param>
    /// <returns>Number of successfully processed configurations</returns>
    Task<int> SetConfigurationsBatchAsync(Dictionary<string, DeviceConfiguration> configurations);
}
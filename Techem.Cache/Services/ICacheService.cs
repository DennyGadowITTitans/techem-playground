using Techem.Cache.Models;

namespace Techem.Cache.Services;

public interface ICacheService
{
    Task<DeviceConfiguration?> GetConfigurationAsync(string prdv);
    Task SetConfigurationAsync(string prdv, DeviceConfiguration configuration);
    Task<bool> ExistsAsync(string prdv);
}
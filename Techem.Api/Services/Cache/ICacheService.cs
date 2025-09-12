using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

public interface ICacheService
{
    Task<DeviceConfiguration?> GetConfigurationAsync(string prdv);
    Task SetConfigurationAsync(string prdv, DeviceConfiguration configuration);
    Task<bool> ExistsAsync(string prdv);
}
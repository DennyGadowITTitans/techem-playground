using Techem.Cache.Models;

namespace Techem.Cache.Services;

public interface IConfigurationDatabaseService
{
    Task<DeviceConfiguration?> GetConfigurationAsync(string prdv);
}
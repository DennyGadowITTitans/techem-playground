using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

public interface IConfigurationDatabaseService
{
    Task<DeviceConfiguration?> GetConfigurationAsync(string prdv);
}
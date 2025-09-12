using Techem.Api.Models;
using Techem.Api.Models.Cache;
using Techem.Api.Services.Cache;

namespace Techem.Api.Services;

public class DummyGdprCheckService : IGdprCheckService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<DummyGdprCheckService> _logger;

    public DummyGdprCheckService(
        ICacheService cacheService, 
        ILogger<DummyGdprCheckService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<DeviceInfo> BuildDeviceInfoAsync(IEnumerable<DataPoint> dataPoints, string? prDv)
    {
        var now = DateTime.UtcNow;

        // Get device configuration from cache service
        DeviceConfiguration? deviceConfig = null;
        if (!string.IsNullOrEmpty(prDv))
        {
            try
            {
                _logger.LogInformation("Getting device configuration from cache, PRDV: {PrDv}", prDv);
                deviceConfig = await _cacheService.GetConfigurationAsync(prDv);
                
                if (deviceConfig != null)
                {
                    _logger.LogInformation("Retrieved device configuration from cache: DeviceType={DeviceType}, StorageEnabled={StorageEnabled}", 
                        deviceConfig.DeviceType, deviceConfig.IsStorageEnabled);
                }
                else
                {
                    _logger.LogWarning("Device configuration not found in cache for PRDV: {PrDv}", prDv);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device configuration from cache for PRDV: {PrDv}", prDv);
            }
        }

        var device = new DeviceInfo
        {
            PrDv = prDv,
            DataPoints = dataPoints.Select(dp => new ServiceInfo
            {
                Uuid = dp.Uuid,
                Mandator = "DEU01",
                Servicetype = "BILL",
                Eventtime = dp.EventTime ?? now,
                Ttl = deviceConfig?.MaxDataAgeInDays ?? 365 // Use config value if available
            }).ToList()
        };

        return device;
    }
}
using Techem.Api.Models;
using Techem.Cache.Protos;

namespace Techem.Api.Services;

public class DummyGdprCheckService : IGdprCheckService
{
    private readonly ConfigurationService.ConfigurationServiceClient _configurationClient;
    private readonly ILogger<DummyGdprCheckService> _logger;

    public DummyGdprCheckService(
        ConfigurationService.ConfigurationServiceClient configurationClient, 
        ILogger<DummyGdprCheckService> logger)
    {
        _configurationClient = configurationClient;
        _logger = logger;
    }

    public async Task<DeviceInfo> BuildDeviceInfoAsync(IEnumerable<DataPoint> dataPoints, string? prDv)
    {
        var now = DateTime.UtcNow;

        // Call Techem.Cache to get device configuration
        DeviceConfiguration? deviceConfig = null;
        if (!string.IsNullOrEmpty(prDv))
        {
            try
            {
                _logger.LogInformation("Calling Techem.Cache for device configuration, PRDV: {PrDv}", prDv);
                var request = new GetConfigurationRequest { Prdv = prDv };
                var response = await _configurationClient.GetConfigurationAsync(request);
                
                if (response.Found && response.Configuration != null)
                {
                    deviceConfig = response.Configuration;
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
                _logger.LogError(ex, "Error calling Techem.Cache for PRDV: {PrDv}", prDv);
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
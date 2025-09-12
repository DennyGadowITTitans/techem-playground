using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

public class DummyConfigurationDatabaseService : IConfigurationDatabaseService
{
    private readonly ILogger<DummyConfigurationDatabaseService> _logger;
    private readonly int _simulatedDelayMs;

    public DummyConfigurationDatabaseService(ILogger<DummyConfigurationDatabaseService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _simulatedDelayMs = configuration.GetValue<int>("DatabaseSimulation:DelayMs", 2000); // Default 2 seconds
    }

    public async Task<DeviceConfiguration?> GetConfigurationAsync(string prdv)
    {
        _logger.LogInformation("Simulating database query for PRDV: {Prdv}", prdv);

        // Simulate database load with artificial delay
        await Task.Delay(_simulatedDelayMs);

        // Return null for unknown PRDVs (simulate not found)
        if (string.IsNullOrEmpty(prdv) || prdv.StartsWith("UNKNOWN"))
        {
            _logger.LogDebug("Configuration not found for PRDV: {Prdv}", prdv);
            return null;
        }

        // Generate realistic device configuration based on PRDV patterns
        var deviceType = GetDeviceTypeFromPrdv(prdv);
        var storageInterval = GetStorageIntervalForDeviceType(deviceType);
        var isStorageEnabled = !prdv.Contains("NOSTORAGE");
        var maxDataAge = GetMaxDataAgeForDeviceType(deviceType);

        var configuration = new DeviceConfiguration
        {
            PrDv = prdv,
            StorageInterval = storageInterval,
            IsStorageEnabled = isStorageEnabled,
            MaxDataAgeInDays = maxDataAge,
            DeviceType = deviceType,
            LastUpdated = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
            AdditionalProperties = GenerateAdditionalProperties(deviceType)
        };

        _logger.LogInformation(
            "Generated configuration for PRDV: {Prdv}, DeviceType: {DeviceType}, StorageInterval: {StorageInterval}, StorageEnabled: {StorageEnabled}",
            prdv, deviceType, storageInterval, isStorageEnabled);

        return configuration;
    }

    private static string GetDeviceTypeFromPrdv(string prdv)
    {
        // Simulate device type detection based on PRDV patterns
        var deviceTypes = new[]
        {
            "HEAT_METER",
            "WATER_METER",
            "GAS_METER",
            "ELECTRICITY_METER",
            "TEMPERATURE_SENSOR",
            "HUMIDITY_SENSOR",
            "PRESSURE_SENSOR",
            "FLOW_SENSOR"
        };

        // Use PRDV hash to consistently assign same device type to same PRDV
        var hash = prdv.GetHashCode();
        var index = Math.Abs(hash) % deviceTypes.Length;
        return deviceTypes[index];
    }

    private static StorageInterval GetStorageIntervalForDeviceType(string deviceType)
    {
        // Different device types have different default storage intervals
        return deviceType switch
        {
            "HEAT_METER" => StorageInterval.Daily,
            "WATER_METER" => StorageInterval.Daily,
            "GAS_METER" => StorageInterval.Daily,
            "ELECTRICITY_METER" => StorageInterval.Hourly,
            "TEMPERATURE_SENSOR" => StorageInterval.Every15Minutes,
            "HUMIDITY_SENSOR" => StorageInterval.Hourly,
            "PRESSURE_SENSOR" => StorageInterval.Daily,
            "FLOW_SENSOR" => StorageInterval.Hourly,
            _ => StorageInterval.Daily
        };
    }

    private static int GetMaxDataAgeForDeviceType(string deviceType)
    {
        // Different device types have different data retention periods
        return deviceType switch
        {
            "HEAT_METER" => 365, // 1 year
            "WATER_METER" => 365, // 1 year
            "GAS_METER" => 365, // 1 year
            "ELECTRICITY_METER" => 180, // 6 months
            "TEMPERATURE_SENSOR" => 90, // 3 months
            "HUMIDITY_SENSOR" => 90, // 3 months
            "PRESSURE_SENSOR" => 180, // 6 months
            "FLOW_SENSOR" => 180, // 6 months
            _ => 30 // Default 30 days
        };
    }

    private static Dictionary<string, string> GenerateAdditionalProperties(string deviceType)
    {
        var properties = new Dictionary<string, string>
        {
            ["DeviceCategory"] = deviceType.Contains("METER") ? "METERING" : "SENSOR",
            ["Vendor"] = "Techem",
            ["FirmwareVersion"] =
                $"v{Random.Shared.Next(1, 5)}.{Random.Shared.Next(0, 10)}.{Random.Shared.Next(0, 99)}",
            ["LastMaintenance"] = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)).ToString("yyyy-MM-dd"),
            ["CriticalDevice"] = (Random.Shared.Next(0, 10) > 7).ToString().ToLower()
        };

        return properties;
    }
}
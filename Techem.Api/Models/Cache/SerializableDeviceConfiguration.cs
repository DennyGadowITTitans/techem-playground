using MessagePack;

namespace Techem.Api.Models.Cache;

/// <summary>
/// Serializable version of DeviceConfiguration optimized for Azure Table Storage throughput
/// Uses MessagePack for efficient serialization while maintaining compatibility with existing DeviceConfiguration
/// </summary>
[MessagePackObject]
public class SerializableDeviceConfiguration
{
    /// <summary>
    /// PRDV identifier of the device
    /// </summary>
    [Key(0)]
    public string PrDv { get; set; } = string.Empty;
    
    /// <summary>
    /// Storage interval configuration for the device (stored as integer for efficiency)
    /// </summary>
    [Key(1)]
    public int StorageIntervalValue { get; set; }
    
    /// <summary>
    /// Whether data storage is enabled for this device
    /// </summary>
    [Key(2)]
    public bool IsStorageEnabled { get; set; } = true;
    
    /// <summary>
    /// Maximum age of data that can be stored (in days)
    /// </summary>
    [Key(3)]
    public int MaxDataAgeInDays { get; set; } = 30;
    
    /// <summary>
    /// Device type for configuration categorization
    /// </summary>
    [Key(4)]
    public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Last configuration update timestamp (as ticks for efficiency)
    /// </summary>
    [Key(5)]
    public long LastUpdatedTicks { get; set; }
    
    /// <summary>
    /// Additional configuration properties
    /// </summary>
    [Key(6)]
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();

    /// <summary>
    /// Convert from DeviceConfiguration to SerializableDeviceConfiguration
    /// </summary>
    /// <param name="config">The DeviceConfiguration to convert</param>
    /// <returns>SerializableDeviceConfiguration instance</returns>
    public static SerializableDeviceConfiguration FromDeviceConfiguration(DeviceConfiguration config)
    {
        return new SerializableDeviceConfiguration
        {
            PrDv = config.PrDv,
            StorageIntervalValue = (int)config.StorageInterval,
            IsStorageEnabled = config.IsStorageEnabled,
            MaxDataAgeInDays = config.MaxDataAgeInDays,
            DeviceType = config.DeviceType,
            LastUpdatedTicks = config.LastUpdated.Ticks,
            AdditionalProperties = new Dictionary<string, string>(config.AdditionalProperties)
        };
    }

    /// <summary>
    /// Convert to DeviceConfiguration
    /// </summary>
    /// <returns>DeviceConfiguration instance</returns>
    public DeviceConfiguration ToDeviceConfiguration()
    {
        return new DeviceConfiguration
        {
            PrDv = PrDv,
            StorageInterval = (StorageInterval)StorageIntervalValue,
            IsStorageEnabled = IsStorageEnabled,
            MaxDataAgeInDays = MaxDataAgeInDays,
            DeviceType = DeviceType,
            LastUpdated = new DateTime(LastUpdatedTicks),
            AdditionalProperties = new Dictionary<string, string>(AdditionalProperties)
        };
    }
}
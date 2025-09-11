namespace Techem.Cache.Models;

/// <summary>
/// Device configuration containing storage rules and intervals for data persistence decisions
/// </summary>
public class DeviceConfiguration
{
    /// <summary>
    /// PRDV identifier of the device
    /// </summary>
    public string PrDv { get; set; } = string.Empty;
    
    /// <summary>
    /// Storage interval configuration for the device
    /// </summary>
    public StorageInterval StorageInterval { get; set; } = StorageInterval.Daily;
    
    /// <summary>
    /// Whether data storage is enabled for this device
    /// </summary>
    public bool IsStorageEnabled { get; set; } = true;
    
    /// <summary>
    /// Maximum age of data that can be stored (in days)
    /// </summary>
    public int MaxDataAgeInDays { get; set; } = 30;
    
    /// <summary>
    /// Device type for configuration categorization
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Last configuration update timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional configuration properties
    /// </summary>
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();
}
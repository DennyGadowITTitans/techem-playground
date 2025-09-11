namespace Techem.Cache.Models;

/// <summary>
/// Storage interval enumeration defining how often data can be stored
/// </summary>
public enum StorageInterval
{
    /// <summary>
    /// Data can be stored every 15 minutes
    /// </summary>
    Every15Minutes = 0,
    
    /// <summary>
    /// Data can be stored hourly
    /// </summary>
    Hourly = 1,
    
    /// <summary>
    /// Data can be stored daily
    /// </summary>
    Daily = 2,
    
    /// <summary>
    /// Data can be stored weekly
    /// </summary>
    Weekly = 3,
    
    /// <summary>
    /// Data can be stored every 15 days
    /// </summary>
    Every15Days = 4,
    
    /// <summary>
    /// Data can be stored monthly
    /// </summary>
    Monthly = 5,
    
    /// <summary>
    /// No storage allowed
    /// </summary>
    NoStorage = 99
}
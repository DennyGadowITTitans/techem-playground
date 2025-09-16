using Azure;
using Azure.Data.Tables;
using MessagePack;
using Techem.Api.Models.Cache;

namespace Techem.Api.Services.Cache;

/// <summary>
/// Azure Table entity representation of DeviceConfiguration with MessagePack serialization for improved throughput
/// </summary>
public class DeviceConfigurationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    
    // Custom properties - only serialized data and expiration
    public byte[]? SerializedData { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public static DeviceConfigurationEntity FromDeviceConfiguration(DeviceConfiguration config, string prdv)
    {
        try
        {
            // Convert to serializable format and serialize with MessagePack
            var serializableConfig = SerializableDeviceConfiguration.FromDeviceConfiguration(config);
            var serializedData = MessagePackSerializer.Serialize(serializableConfig);
            
            return new DeviceConfigurationEntity
            {
                PartitionKey = "config",
                RowKey = prdv,
                SerializedData = serializedData
            };
        }
        catch (Exception)
        {
            // If MessagePack serialization fails, throw to indicate the error
            throw new InvalidOperationException($"Failed to serialize DeviceConfiguration for PRDV: {prdv}");
        }
    }

    public DeviceConfiguration ToDeviceConfiguration()
    {
        try
        {
            if (SerializedData == null || SerializedData.Length == 0)
            {
                throw new InvalidOperationException($"No serialized data available for PRDV: {RowKey}");
            }

            // Deserialize with MessagePack and convert back to DeviceConfiguration
            var serializableConfig = MessagePackSerializer.Deserialize<SerializableDeviceConfiguration>(SerializedData);
            return serializableConfig.ToDeviceConfiguration();
        }
        catch (Exception)
        {
            // If deserialization fails, throw to indicate the error
            throw new InvalidOperationException($"Failed to deserialize DeviceConfiguration for PRDV: {RowKey}");
        }
    }
}
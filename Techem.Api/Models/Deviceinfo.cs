namespace Techem.Api.Models;

/// <summary>
/// DeviceInfo result as defined in the OpenAPI spec.
/// </summary>
public class DeviceInfo
{
    public string? PrDv { get; set; }
    public List<ServiceInfo> DataPoints { get; set; } = [];
}
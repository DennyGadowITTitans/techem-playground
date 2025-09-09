namespace Techem.Api.Models;

/// <summary>
/// ServiceInfo item as defined in the OpenAPI spec.
/// </summary>
public class ServiceInfo
{
    public string Uuid { get; set; } = string.Empty;
    public string? Mandator { get; set; }
    public string? Servicetype { get; set; }
    public DateTime? Eventtime { get; set; }
    public int? Ttl { get; set; }
}
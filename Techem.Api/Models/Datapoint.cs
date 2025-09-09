using System.ComponentModel.DataAnnotations;

namespace Techem.Api.Models;

/// <summary>
/// Datapoint as defined in the OpenAPI spec.
/// </summary>
public class DataPoint
{
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Subunit { get; set; }
    public string? Tariff { get; set; }
    public string? Function { get; set; }
    public DateTime? EventTime { get; set; }
    [Required]
    public string Uuid { get; set; } = string.Empty;
}
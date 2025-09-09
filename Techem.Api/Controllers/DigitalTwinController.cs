using Microsoft.AspNetCore.Mvc;
using Techem.Api.Models;
using Techem.Api.Services;

namespace Techem.Api.Controllers;

[ApiController]
[Route("device")] // matches /device/serviceinfo
public class DigitalTwinController(IGdprCheckService service) : ControllerBase
{
    /// <summary>
    /// Finds device based on the PRDV ID of the device. Delivers associated service assignments per data point.
    /// </summary>
    /// <remarks>
    /// This endpoint accepts a list of DataPoints for which service info is requested and returns the device info with
    /// service assignments per datapoint.
    /// </remarks>
    /// <param name="body">List of DataPoints for which service info is requested.</param>
    /// <param name="prDv">PRDV ID of the Device (optional). Query parameter name in spec is 'prdv'.</param>
    /// <returns>DeviceInfo object containing the PRDV and service assignments per datapoint.</returns>
    [HttpPost("service-info")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DeviceInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Tags("DigitalTwin")] // matches spec tag
    public async Task<IActionResult> FindServiceBySerial([FromBody] IReadOnlyList<DataPoint>? body, [FromQuery(Name = "prdv")] string? prDv)
    {
        if (body == null)
        {
            return BadRequest("Body must be an array of Datapoint");
        }
        var missingUuid = body.FirstOrDefault(dp => string.IsNullOrWhiteSpace(dp.Uuid));
        if (missingUuid != null)
        {
            return BadRequest("Each datapoint must include a non-empty uuid");
        }
        var device = await service.BuildDeviceInfoAsync(body, prDv);
        return Ok(device);
    }
}
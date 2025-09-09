using Techem.Api.Models;

namespace Techem.Api.Services;

public interface IGdprCheckService
{
    Task<DeviceInfo> BuildDeviceInfoAsync(IEnumerable<DataPoint> dataPoints, string? prDv);
}
using Techem.Api.Models;

namespace Techem.Api.Services;

public class DummyGdprCheckService : IGdprCheckService
{
    public async Task<DeviceInfo> BuildDeviceInfoAsync(IEnumerable<DataPoint> dataPoints, string? prDv)
    {
        var now = DateTime.UtcNow;
        var device = new DeviceInfo
        {
            PrDv = prDv,
            DataPoints = dataPoints.Select(dp => new ServiceInfo
            {
                Uuid = dp.Uuid,
                Mandator = "DEU01",
                Servicetype = "BILL",
                Eventtime = dp.EventTime ?? now,
                Ttl = 365
            }).ToList()
        };
        return await Task.FromResult(device);
    }
}
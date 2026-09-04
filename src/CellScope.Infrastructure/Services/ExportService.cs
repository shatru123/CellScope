using System.Text;
using System.Text.Json;
using CellScope.Application.Interfaces;
using CellScope.Application.Mapping;
using CellScope.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CellScope.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly CellScopeDbContext _dbContext;

    public ExportService(CellScopeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> ExportAsCsvAsync(string dataType = "everything", CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        if (dataType.Equals("cellular", StringComparison.OrdinalIgnoreCase) || dataType.Equals("everything", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("# Cellular Observations");
            sb.AppendLine("Timestamp,DeviceId,Operator,Technology,CellId,TAC,PCI,Band,Frequency,SignalStrengthDbm,SignalQuality,Latitude,Longitude,DataSource");

            var snapshots = await _dbContext.CellularSnapshots
                .AsNoTracking()
                .OrderByDescending(s => s.Timestamp)
                .Take(500)
                .ToListAsync(cancellationToken);

            foreach (var s in snapshots)
            {
                sb.AppendLine($"\"{s.Timestamp:O}\",\"{s.DeviceId}\",\"{s.OperatorName}\",\"{s.RadioTechnology}\",\"{s.CellId}\",\"{s.TrackingAreaCode}\",\"{s.PhysicalCellId}\",\"{s.Band}\",\"{s.Frequency}\",{s.SignalStrengthDbm},{s.SignalQuality},{s.Latitude},{s.Longitude},\"{s.DataSource}\"");
            }
            sb.AppendLine();
        }

        if (dataType.Equals("locations", StringComparison.OrdinalIgnoreCase) || dataType.Equals("everything", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("# Location Trail");
            sb.AppendLine("Timestamp,DeviceId,Latitude,Longitude,Accuracy,Altitude,Speed,Bearing");

            var locations = await _dbContext.LocationPoints
                .AsNoTracking()
                .OrderBy(l => l.Timestamp)
                .Take(1000)
                .ToListAsync(cancellationToken);

            foreach (var l in locations)
            {
                sb.AppendLine($"\"{l.Timestamp:O}\",\"{l.DeviceId}\",{l.Latitude},{l.Longitude},{l.Accuracy},{l.Altitude},{l.Speed},{l.Bearing}");
            }
            sb.AppendLine();
        }

        if (dataType.Equals("devices", StringComparison.OrdinalIgnoreCase) || dataType.Equals("everything", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("# Local Network Devices");
            sb.AppendLine("FirstSeen,LastSeen,IpAddress,MacAddress,Hostname,Vendor,DeviceType,IsOnline,ResponseTimeMs");

            var devices = await _dbContext.NetworkDevices
                .AsNoTracking()
                .OrderByDescending(d => d.LastSeen)
                .Take(200)
                .ToListAsync(cancellationToken);

            foreach (var d in devices)
            {
                sb.AppendLine($"\"{d.FirstSeen:O}\",\"{d.LastSeen:O}\",\"{d.IpAddress}\",\"{d.MacAddress}\",\"{d.Hostname}\",\"{d.Vendor}\",\"{d.DeviceType}\",{d.IsOnline},{d.ResponseTimeMs}");
            }
        }

        return sb.ToString();
    }

    public async Task<string> ExportAsJsonAsync(string dataType = "everything", CancellationToken cancellationToken = default)
    {
        var exportObj = new Dictionary<string, object>();

        if (dataType.Equals("cellular", StringComparison.OrdinalIgnoreCase) || dataType.Equals("everything", StringComparison.OrdinalIgnoreCase))
        {
            var snapshots = await _dbContext.CellularSnapshots
                .Include(s => s.NeighborCells)
                .AsNoTracking()
                .OrderByDescending(s => s.Timestamp)
                .Take(500)
                .ToListAsync(cancellationToken);

            exportObj["cellularSnapshots"] = snapshots.Select(DtoMapper.ToDto).ToList();
        }

        if (dataType.Equals("locations", StringComparison.OrdinalIgnoreCase) || dataType.Equals("everything", StringComparison.OrdinalIgnoreCase))
        {
            var locations = await _dbContext.LocationPoints
                .AsNoTracking()
                .OrderBy(l => l.Timestamp)
                .Take(1000)
                .ToListAsync(cancellationToken);

            exportObj["locationTrail"] = locations.Select(DtoMapper.ToDto).ToList();
        }

        if (dataType.Equals("devices", StringComparison.OrdinalIgnoreCase) || dataType.Equals("everything", StringComparison.OrdinalIgnoreCase))
        {
            var devices = await _dbContext.NetworkDevices
                .AsNoTracking()
                .OrderByDescending(d => d.LastSeen)
                .Take(200)
                .ToListAsync(cancellationToken);

            exportObj["networkDevices"] = devices.Select(DtoMapper.ToDto).ToList();
        }

        return JsonSerializer.Serialize(exportObj, new JsonSerializerOptions { WriteIndented = true });
    }
}

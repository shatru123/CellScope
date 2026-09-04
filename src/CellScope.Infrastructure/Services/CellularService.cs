using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Application.Mapping;
using CellScope.Domain.Entities;
using CellScope.Domain.Services;
using CellScope.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CellScope.Infrastructure.Services;

public class CellularService : ICellularService
{
    private readonly CellScopeDbContext _dbContext;
    private readonly INotificationPublisher _notifier;

    public CellularService(CellScopeDbContext dbContext, INotificationPublisher notifier)
    {
        _dbContext = dbContext;
        _notifier = notifier;
    }

    public async Task<CellularSnapshotDto> IngestSnapshotAsync(IngestSnapshotRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Find device or auto-register fallback device
        var device = await _dbContext.Devices.FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);
        if (device == null)
        {
            device = new Device
            {
                Id = request.DeviceId,
                Name = "Android Collector",
                Platform = "Android",
                LastSeenAt = DateTimeOffset.UtcNow,
                IsPaired = true
            };
            _dbContext.Devices.Add(device);
        }
        else
        {
            device.LastSeenAt = DateTimeOffset.UtcNow;
        }

        // 2. Fetch previous snapshot to detect handovers
        var previousSnapshot = await _dbContext.CellularSnapshots
            .Where(s => s.DeviceId == request.DeviceId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        // 3. Construct current snapshot
        var snapshot = new CellularSnapshot
        {
            DeviceId = request.DeviceId,
            Timestamp = request.Timestamp ?? DateTimeOffset.UtcNow,
            OperatorName = request.OperatorName,
            Mcc = request.Mcc,
            Mnc = request.Mnc,
            RadioTechnology = request.RadioTechnology,
            CellId = request.CellId,
            TrackingAreaCode = request.TrackingAreaCode,
            PhysicalCellId = request.PhysicalCellId,
            Frequency = request.Frequency,
            Band = request.Band,
            SignalStrengthDbm = request.SignalStrengthDbm,
            SignalLevel = request.SignalLevel,
            SignalQuality = request.SignalQuality,
            IsRegistered = request.IsRegistered,
            IsRoaming = request.IsRoaming,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationAccuracy = request.LocationAccuracy,
            Altitude = request.Altitude,
            DataSource = request.DataSource ?? "Android:TelephonyManager",
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (request.NeighborCells.Count > 0)
        {
            snapshot.NeighborCells = request.NeighborCells.Select(n => new NeighborCell
            {
                SnapshotId = snapshot.Id,
                CellId = n.CellId,
                PhysicalCellId = n.PhysicalCellId,
                TrackingAreaCode = n.TrackingAreaCode,
                RadioTechnology = n.RadioTechnology,
                Band = n.Band,
                Frequency = n.Frequency,
                SignalStrengthDbm = n.SignalStrengthDbm,
                SignalQuality = n.SignalQuality,
                IsRegistered = n.IsRegistered
            }).ToList();
        }

        _dbContext.CellularSnapshots.Add(snapshot);

        // 4. Record Signal Observation
        if (snapshot.SignalStrengthDbm.HasValue)
        {
            _dbContext.SignalObservations.Add(new SignalObservation
            {
                DeviceId = snapshot.DeviceId,
                Timestamp = snapshot.Timestamp,
                SignalStrengthDbm = snapshot.SignalStrengthDbm.Value,
                SignalQuality = snapshot.SignalQuality,
                RadioTechnology = snapshot.RadioTechnology,
                OperatorName = snapshot.OperatorName
            });
        }

        // 5. Record Location Point
        if (snapshot.Latitude.HasValue && snapshot.Longitude.HasValue)
        {
            _dbContext.LocationPoints.Add(new LocationPoint
            {
                DeviceId = snapshot.DeviceId,
                Latitude = snapshot.Latitude.Value,
                Longitude = snapshot.Longitude.Value,
                Accuracy = snapshot.LocationAccuracy,
                Altitude = snapshot.Altitude,
                Timestamp = snapshot.Timestamp
            });
        }

        // 6. Check for handover
        CellHandover? handover = null;
        if (previousSnapshot != null)
        {
            handover = HandoverDetector.CheckForHandover(previousSnapshot, snapshot);
            if (handover != null)
            {
                _dbContext.CellHandovers.Add(handover);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = DtoMapper.ToDto(snapshot);

        // 7. Publish live SignalR notifications
        await _notifier.PublishSnapshotAsync(dto, cancellationToken);
        if (handover != null)
        {
            await _notifier.PublishHandoverAsync(DtoMapper.ToDto(handover), cancellationToken);
        }

        return dto;
    }

    public async Task<CellularSnapshotDto?> GetCurrentSnapshotAsync(Guid? deviceId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.CellularSnapshots
                .Include(s => s.NeighborCells)
                .AsNoTracking();

            if (deviceId.HasValue)
                query = query.Where(s => s.DeviceId == deviceId.Value);

            var snapshot = await query
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            return snapshot != null ? DtoMapper.ToDto(snapshot) : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CellularService Notice] GetCurrentSnapshotAsync fallback: {ex.Message}");
            return null;
        }
    }

    public async Task<IReadOnlyList<CellularSnapshotDto>> GetHistoryAsync(Guid? deviceId = null, int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.CellularSnapshots
                .Include(s => s.NeighborCells)
                .AsNoTracking();

            if (deviceId.HasValue)
                query = query.Where(s => s.DeviceId == deviceId.Value);

            var list = await query
                .OrderByDescending(s => s.Timestamp)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return list.Select(DtoMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CellularService Notice] GetHistoryAsync fallback: {ex.Message}");
            return Array.Empty<CellularSnapshotDto>();
        }
    }

    public async Task<IReadOnlyList<NeighborCellDto>> GetCurrentNeighborsAsync(Guid? deviceId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var current = await GetCurrentSnapshotAsync(deviceId, cancellationToken);
            return current?.NeighborCells ?? new List<NeighborCellDto>();
        }
        catch
        {
            return Array.Empty<NeighborCellDto>();
        }
    }

    public async Task<IReadOnlyList<CellHandoverDto>> GetHandoversAsync(Guid? deviceId = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.CellHandovers.AsNoTracking();
            if (deviceId.HasValue)
                query = query.Where(h => h.DeviceId == deviceId.Value);

            var list = await query
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return list.Select(DtoMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CellularService Notice] GetHandoversAsync fallback: {ex.Message}");
            return Array.Empty<CellHandoverDto>();
        }
    }

    public async Task<IReadOnlyList<LocationPointDto>> GetLocationTrailAsync(Guid? deviceId = null, int limit = 200, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.LocationPoints.AsNoTracking();
            if (deviceId.HasValue)
                query = query.Where(l => l.DeviceId == deviceId.Value);

            var list = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return list.OrderBy(l => l.Timestamp).Select(DtoMapper.ToDto).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CellularService Notice] GetLocationTrailAsync fallback: {ex.Message}");
            return Array.Empty<LocationPointDto>();
        }
    }
}

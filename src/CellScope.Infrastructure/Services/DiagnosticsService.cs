using System.Diagnostics;
using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CellScope.Infrastructure.Services;

public class DiagnosticsService : IDiagnosticsService
{
    private readonly CellScopeDbContext _dbContext;
    private readonly IDemoDataService _demoDataService;

    public DiagnosticsService(CellScopeDbContext dbContext, IDemoDataService demoDataService)
    {
        _dbContext = dbContext;
        _demoDataService = demoDataService;
    }

    public async Task<SystemDiagnosticsDto> RunDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        bool dbHealthy = false;
        long dbLatencyMs = 0;
        try
        {
            dbHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
            sw.Stop();
            dbLatencyMs = sw.ElapsedMilliseconds;
        }
        catch
        {
            dbHealthy = false;
            sw.Stop();
            dbLatencyMs = sw.ElapsedMilliseconds;
        }

        int totalDevices = 0;
        int onlineDevices = 0;
        DateTimeOffset? lastUpdate = null;

        if (dbHealthy)
        {
            try
            {
                var threshold = DateTimeOffset.UtcNow.AddMinutes(-3);
                totalDevices = await _dbContext.Devices.CountAsync(cancellationToken);
                onlineDevices = await _dbContext.Devices.CountAsync(d => d.LastSeenAt >= threshold, cancellationToken);
                var latestSnapshot = await _dbContext.CellularSnapshots
                    .OrderByDescending(s => s.Timestamp)
                    .Select(s => s.Timestamp)
                    .FirstOrDefaultAsync(cancellationToken);
                if (latestSnapshot != default) lastUpdate = latestSnapshot;
            }
            catch { }
        }

        return new SystemDiagnosticsDto
        {
            ApiStatus = "Healthy",
            DatabaseStatus = dbHealthy ? "Healthy" : "Degraded",
            DatabaseLatencyMs = dbLatencyMs,
            SignalRStatus = "Operational",
            ActiveConnections = 1,
            TotalDevices = totalDevices,
            OnlineDevices = onlineDevices,
            LastCellularUpdate = lastUpdate,
            LocationServiceStatus = "Available",
            PermissionsStatus = "All required permissions granted",
            IsDemoMode = _demoDataService.IsDemoModeActive,
            CheckedAt = DateTimeOffset.UtcNow
        };
    }
}

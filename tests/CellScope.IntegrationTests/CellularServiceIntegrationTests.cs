using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Domain.Entities;
using CellScope.Infrastructure.Data;
using CellScope.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CellScope.IntegrationTests;

public class CellularServiceIntegrationTests
{
    private class TestNotifier : INotificationPublisher
    {
        public List<CellularSnapshotDto> Snapshots { get; } = new();
        public List<CellHandoverDto> Handovers { get; } = new();

        public Task PublishSnapshotAsync(CellularSnapshotDto snapshot, CancellationToken cancellationToken = default)
        {
            Snapshots.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task PublishHandoverAsync(CellHandoverDto handover, CancellationToken cancellationToken = default)
        {
            Handovers.Add(handover);
            return Task.CompletedTask;
        }

        public Task PublishDeviceStatusAsync(DeviceDto device, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishNetworkScanAsync(LocalNetworkDto network, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task IngestSnapshot_DetectsHandoverAndPublishesLiveUpdate()
    {
        var options = new DbContextOptionsBuilder<CellScopeDbContext>()
            .UseSqlite($"Data Source=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        using var db = new CellScopeDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        var notifier = new TestNotifier();
        var service = new CellularService(db, notifier);

        var deviceId = Guid.NewGuid();

        // Snapshot 1: Cell 12345
        var snap1 = await service.IngestSnapshotAsync(new IngestSnapshotRequest
        {
            DeviceId = deviceId,
            CellId = "310410_12345",
            RadioTechnology = "5G NR",
            SignalStrengthDbm = -85,
            Latitude = 37.7749,
            Longitude = -122.4194
        });

        // Snapshot 2: Cell 98765 (triggers handover)
        var snap2 = await service.IngestSnapshotAsync(new IngestSnapshotRequest
        {
            DeviceId = deviceId,
            CellId = "310410_98765",
            RadioTechnology = "5G NR",
            SignalStrengthDbm = -75,
            Latitude = 37.7776,
            Longitude = -122.4150
        });

        notifier.Snapshots.Should().HaveCount(2);
        notifier.Handovers.Should().HaveCount(1);
        notifier.Handovers[0].PreviousCellId.Should().Be("310410_12345");
        notifier.Handovers[0].NewCellId.Should().Be("310410_98765");

        var handoversInDb = await service.GetHandoversAsync(deviceId);
        handoversInDb.Should().HaveCount(1);
    }
}

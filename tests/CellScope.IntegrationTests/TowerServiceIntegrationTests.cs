using CellScope.Infrastructure.Data;
using CellScope.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CellScope.IntegrationTests;

public class TowerServiceIntegrationTests
{
    [Fact]
    public async Task GetNearbyTowers_FiltersByRadius()
    {
        var options = new DbContextOptionsBuilder<CellScopeDbContext>()
            .UseSqlite($"Data Source=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        using var db = new CellScopeDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        var towerService = new TowerService(db);
        await towerService.SeedDefaultTowersAsync();

        // Query near SF Market St
        var nearby = await towerService.GetNearbyTowersAsync(37.7749, -122.4194, radiusMeters: 2000);

        nearby.Should().NotBeEmpty();
        nearby.All(t => t.DistanceMeters <= 2000).Should().BeTrue();
        nearby.Any(t => t.ConnectedDevices.Count > 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetConnectedDevicesForTower_ReturnsSubscribers()
    {
        var options = new DbContextOptionsBuilder<CellScopeDbContext>()
            .UseSqlite($"Data Source=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        using var db = new CellScopeDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        var towerService = new TowerService(db);
        await towerService.SeedDefaultTowersAsync();

        var devices = await towerService.GetConnectedDevicesForTowerAsync("310410_12345");
        devices.Should().NotBeNull();
        devices.Should().NotBeEmpty();
        devices[0].DeviceName.Should().NotBeNullOrWhiteSpace();
        devices[0].ConnectionState.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetActiveCallsForTower_ReturnsOngoingVoiceAndVideoCalls()
    {
        var options = new DbContextOptionsBuilder<CellScopeDbContext>()
            .UseSqlite($"Data Source=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        using var db = new CellScopeDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        var towerService = new TowerService(db);
        var calls = await towerService.GetActiveCallsForTowerAsync("310410_12345");

        calls.Should().NotBeNull();
        calls.Should().NotBeEmpty();
        calls.Should().HaveCountGreaterThanOrEqualTo(10);
        calls.All(c => !string.IsNullOrEmpty(c.CallerNumber) && !string.IsNullOrEmpty(c.ReceiverNumber)).Should().BeTrue();
        calls.All(c => c.MosScore >= 1.0 && c.MosScore <= 5.0).Should().BeTrue();
        calls.Any(c => c.CallType.Contains("VoNR") || c.CallType.Contains("VoLTE")).Should().BeTrue();
    }
}



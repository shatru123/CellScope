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
    }
}

using CellScope.Domain.Entities;
using CellScope.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CellScope.IntegrationTests;

public class DbContextTests
{
    private CellScopeDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CellScopeDbContext>()
            .UseSqlite($"Data Source=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        var context = new CellScopeDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CanAddAndQuery_CellularSnapshotWithNeighbors()
    {
        using var db = CreateInMemoryDbContext();

        var device = new Device { Name = "Test Device", Platform = "Android" };
        db.Devices.Add(device);
        await db.SaveChangesAsync();

        var snapshot = new CellularSnapshot
        {
            DeviceId = device.Id,
            OperatorName = "Airtel / Telecom",
            Mcc = 310,
            Mnc = 410,
            RadioTechnology = "5G NR",
            CellId = "310410_12345",
            SignalStrengthDbm = -80,
            Latitude = 37.7749,
            Longitude = -122.4194
        };

        snapshot.NeighborCells.Add(new NeighborCell
        {
            CellId = "310410_98765",
            PhysicalCellId = "204",
            RadioTechnology = "5G NR",
            SignalStrengthDbm = -86
        });

        db.CellularSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var retrieved = await db.CellularSnapshots
            .Include(s => s.NeighborCells)
            .FirstOrDefaultAsync(s => s.Id == snapshot.Id);

        retrieved.Should().NotBeNull();
        retrieved!.OperatorName.Should().Be("Airtel / Telecom");
        retrieved.NeighborCells.Should().HaveCount(1);
        retrieved.NeighborCells.First().CellId.Should().Be("310410_98765");
    }
}

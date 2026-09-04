using CellScope.Domain.Entities;
using CellScope.Domain.Services;
using FluentAssertions;
using Xunit;

namespace CellScope.UnitTests;

public class HandoverDetectorTests
{
    [Fact]
    public void CheckForHandover_CellIdChanged_ReturnsHandover()
    {
        var prev = new CellularSnapshot
        {
            DeviceId = Guid.NewGuid(),
            CellId = "310410_12345",
            RadioTechnology = "5G NR",
            SignalStrengthDbm = -85
        };

        var current = new CellularSnapshot
        {
            DeviceId = prev.DeviceId,
            CellId = "310410_98765",
            RadioTechnology = "5G NR",
            SignalStrengthDbm = -74,
            Latitude = 37.7776,
            Longitude = -122.4150
        };

        var handover = HandoverDetector.CheckForHandover(prev, current);

        handover.Should().NotBeNull();
        handover!.PreviousCellId.Should().Be("310410_12345");
        handover.NewCellId.Should().Be("310410_98765");
        handover.PreviousSignalDbm.Should().Be(-85);
        handover.NewSignalDbm.Should().Be(-74);
        handover.TriggerReason.Should().Contain("Serving cell");
    }

    [Fact]
    public void CheckForHandover_SameCellAndTech_ReturnsNull()
    {
        var prev = new CellularSnapshot
        {
            DeviceId = Guid.NewGuid(),
            CellId = "310410_12345",
            RadioTechnology = "5G NR",
            SignalStrengthDbm = -85
        };

        var current = new CellularSnapshot
        {
            DeviceId = prev.DeviceId,
            CellId = "310410_12345",
            RadioTechnology = "5G NR",
            SignalStrengthDbm = -82
        };

        var handover = HandoverDetector.CheckForHandover(prev, current);
        handover.Should().BeNull();
    }
}

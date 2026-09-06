using CellScope.Domain.Services;
using FluentAssertions;
using Xunit;

namespace CellScope.UnitTests;

public class GeodesyUtilsTests
{
    [Fact]
    public void CalculateDistanceMeters_SameCoordinates_ReturnsZero()
    {
        double distance = GeodesyUtils.CalculateDistanceMeters(37.7749, -122.4194, 37.7749, -122.4194);
        distance.Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void CalculateDistanceMeters_KnownCoordinates_ReturnsAccurateDistance()
    {
        // San Francisco (Market St) to SF Ferry Building (~2.5 km)
        double lat1 = 37.7749, lon1 = -122.4194;
        double lat2 = 37.7955, lon2 = -122.3937;

        double distance = GeodesyUtils.CalculateDistanceMeters(lat1, lon1, lat2, lon2);
        distance.Should().BeInRange(3000, 3300);
    }

    [Fact]
    public void GetBoundingBox_ContainsCenterPoint()
    {
        double lat = 37.7749, lon = -122.4194;
        double radius = 1000; // 1km

        var (minLat, maxLat, minLon, maxLon) = GeodesyUtils.GetBoundingBox(lat, lon, radius);

        lat.Should().BeInRange(minLat, maxLat);
        lon.Should().BeInRange(minLon, maxLon);
        (maxLat - minLat).Should().BeGreaterThan(0);
        (maxLon - minLon).Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateBearing_NorthDirection_ReturnsNearZero()
    {
        // Moving strictly North
        double lat1 = 18.50, lon1 = 73.80;
        double lat2 = 18.60, lon2 = 73.80;

        double bearing = GeodesyUtils.CalculateBearing(lat1, lon1, lat2, lon2);
        bearing.Should().BeApproximately(0.0, 1.0);
    }

    [Fact]
    public void GetCompassDirection_CardinalDirections_ReturnsCorrectNames()
    {
        // Lat 18.5, Lon 73.8 to East:
        string eastDir = GeodesyUtils.GetCompassDirection(18.5, 73.8, 18.5, 73.9);
        eastDir.Should().Be("East");

        // Lat 18.5, Lon 73.8 to South:
        string southDir = GeodesyUtils.GetCompassDirection(18.5, 73.8, 18.4, 73.8);
        southDir.Should().Be("South");

        // Lat 18.5, Lon 73.8 to West:
        string westDir = GeodesyUtils.GetCompassDirection(18.5, 73.8, 18.5, 73.7);
        westDir.Should().Be("West");
    }
}

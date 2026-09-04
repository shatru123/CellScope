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
}

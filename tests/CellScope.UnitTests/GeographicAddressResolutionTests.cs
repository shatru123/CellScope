using CellScope.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CellScope.UnitTests;

public class GeographicAddressResolutionTests
{
    [Fact]
    public void ResolveGeographicAddress_HinjewadiCoordinates_ResolvesToHinjewadiPune()
    {
        // Hinjewadi Phase 1 center coordinates
        double lat = 18.5913;
        double lon = 73.7389;

        var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, 0, "5G NR");

        area.Should().Contain("Hinjewadi");
        city.Should().Be("Pune");
        postal.Should().Be("MH 411057");
        street.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ResolveGeographicAddress_HinjewadiPhase3Coordinates_NeverResolvesToSwargate()
    {
        // Hinjewadi Phase 3 Megapolis (~25 km from Swargate)
        double lat = 18.5975;
        double lon = 73.7150;

        // Even with various indices that previously mapped to Swargate via modulo
        for (int idx = 0; idx < 20; idx++)
        {
            var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, idx, "LTE");

            area.Should().Contain("Hinjewadi");
            area.Should().NotContain("Swargate");
            area.Should().NotContain("Hadapsar");
            city.Should().Be("Pune");
        }
    }

    [Fact]
    public void ResolveGeographicAddress_BkcCoordinates_ResolvesToBkcMumbai()
    {
        // BKC G-Block center coordinates
        double lat = 19.0664;
        double lon = 72.8682;

        for (int idx = 0; idx < 15; idx++)
        {
            var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, idx, "5G NR");

            area.Should().Be("BKC (Bandra Kurla Complex)");
            city.Should().Be("Mumbai");
            postal.Should().Be("MH 400051");
            area.Should().NotContain("Colaba");
        }
    }

    [Fact]
    public void ResolveGeographicAddress_CyberCityGurugram_ResolvesToGurugramDelhiNcr()
    {
        // DLF Cyber City Gurugram
        double lat = 28.4950;
        double lon = 77.0890;

        var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, 2, "5G NR");

        area.Should().Contain("Cyber City");
        city.Should().Be("Gurugram");
        postal.Should().Be("HR 122002");
    }

    [Fact]
    public void ResolveGeographicAddress_Koramangala_ResolvesToBengaluru()
    {
        // Koramangala 80 Feet Road
        double lat = 12.9352;
        double lon = 77.6245;

        var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, 1, "LTE");

        area.Should().Contain("Koramangala");
        city.Should().Be("Bengaluru");
        postal.Should().Be("KA 560034");
    }

    [Fact]
    public void ResolveGeographicAddress_HitecCity_ResolvesToHyderabad()
    {
        // HITEC City Cyber Towers
        double lat = 17.4504;
        double lon = 78.3808;

        var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, 0, "5G NR");

        area.Should().Contain("HITEC City");
        city.Should().Be("Hyderabad");
        postal.Should().Be("TG 500081");
    }

    [Fact]
    public void ResolveGeographicAddress_OmrChennai_ResolvesToChennai()
    {
        // OMR IT Corridor
        double lat = 12.9890;
        double lon = 80.2470;

        var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, 3, "LTE");

        area.Should().Contain("OMR IT");
        city.Should().Be("Chennai");
        postal.Should().Be("TN 600113");
    }

    [Fact]
    public void ResolveGeographicAddress_SaltLake_ResolvesToKolkata()
    {
        // Salt Lake Sector V
        double lat = 22.5800;
        double lon = 88.4320;

        var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, 0, "5G NR");

        area.Should().Contain("Salt Lake");
        city.Should().Be("Kolkata");
        postal.Should().Be("WB 700091");
    }

    [Fact]
    public void ResolveGeographicAddress_MicroDistanceOffset_GeneratesAccurateCompassSector()
    {
        // 500m North-East of Hinjewadi Phase 1
        double lat = 18.5913 + 0.003;
        double lon = 73.7389 + 0.003;

        var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, 0, "5G NR");

        area.Should().Contain("Hinjewadi");
        street.Should().Contain("Sector");
        street.Should().Contain("Rajiv Gandhi Infotech Park");
    }

    [Fact]
    public void ResolveGeographicAddress_ArbitraryRemoteCoordinates_ResolvesCleanly()
    {
        // Middle of the ocean / remote coordinates
        double lat = -15.421;
        double lon = -130.884;

        var (area, street, city, postal) = DemoDataService.ResolveGeographicAddress(lat, lon, 5, "LTE");

        area.Should().NotBeNullOrWhiteSpace();
        street.Should().NotBeNullOrWhiteSpace();
        city.Should().Contain("Regional Telecom Grid");
        postal.Should().StartWith("LOC-");
    }
}

using CellScope.Application.DTOs;
using CellScope.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CellScope.UnitTests;

public class GisExportTests
{
    private readonly GisExportService _service = new();

    private List<TowerLocationDto> CreateSampleTowers() =>
    [
        new TowerLocationDto
        {
            Id = Guid.NewGuid(),
            CellId = "404-45-12345",
            OperatorName = "Airtel 5G",
            RadioTechnology = "5G NR",
            Latitude = 18.5913,
            Longitude = 73.7389,
            RangeMeters = 850,
            Area = "Hinjewadi Phase 1",
            City = "Pune",
            StreetAddress = "Rajiv Gandhi Infotech Park",
            Confidence = "High"
        },
        new TowerLocationDto
        {
            Id = Guid.NewGuid(),
            CellId = "404-86-67890",
            OperatorName = "Jio True 5G",
            RadioTechnology = "5G NR",
            Latitude = 18.5204,
            Longitude = 73.8567,
            RangeMeters = 1200,
            Area = "Shivajinagar",
            City = "Pune",
            StreetAddress = "FC Road",
            Confidence = "High"
        }
    ];

    [Fact]
    public void GenerateGeoJson_ValidTowers_ProducesRfc7946GeoJson()
    {
        var towers = CreateSampleTowers();
        var json = _service.GenerateGeoJson(towers, null, null);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"type\": \"FeatureCollection\"");
        json.Should().Contain("\"type\": \"Feature\"");
        json.Should().Contain("\"type\": \"Point\"");
        json.Should().Contain("18.5913");
        json.Should().Contain("73.7389");
        json.Should().Contain("Airtel 5G");
        json.Should().Contain("Hinjewadi Phase 1");
    }

    [Fact]
    public void GenerateKml_ValidTowers_ProducesGoogleEarthXml()
    {
        var towers = CreateSampleTowers();
        var kml = _service.GenerateKml(towers, null, null);

        kml.Should().NotBeNullOrWhiteSpace();
        kml.Should().Contain("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        kml.Should().Contain("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
        kml.Should().Contain("<Placemark>");
        kml.Should().Contain("<coordinates>73.7389,18.5913,45</coordinates>");
        kml.Should().Contain("Airtel 5G");
    }

    [Fact]
    public void GenerateCsv_ValidTowers_ProducesCsvWithHeaders()
    {
        var towers = CreateSampleTowers();
        var csv = _service.GenerateCsv(towers);

        csv.Should().NotBeNullOrWhiteSpace();
        csv.Should().Contain("CellId,Operator,RadioTechnology,Latitude,Longitude,Area,StreetAddress,City,PostalCode,PCI,RangeMeters,Confidence,Source");
        csv.Should().Contain("404-45-12345");
        csv.Should().Contain("18.5913");
        csv.Should().Contain("Hinjewadi Phase 1");
    }
}

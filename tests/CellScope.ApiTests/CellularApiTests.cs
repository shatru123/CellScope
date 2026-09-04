using System.Net;
using System.Net.Http.Json;
using CellScope.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace CellScope.ApiTests;

public class CellularApiTests : IClassFixture<CellScopeTestFactory>
{
    private readonly HttpClient _client;

    public CellularApiTests(CellScopeTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrent_ReturnsSuccessOrDemoSnapshot()
    {
        var response = await _client.GetAsync("/api/cellular/current");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var snapshot = await response.Content.ReadFromJsonAsync<CellularSnapshotDto>();
        snapshot.Should().NotBeNull();
        snapshot!.RadioTechnology.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task IngestSnapshot_PersistsAndReturnsDto()
    {
        var request = new IngestSnapshotRequest
        {
            DeviceId = Guid.NewGuid(),
            OperatorName = "Test Carrier",
            Mcc = 310,
            Mnc = 410,
            RadioTechnology = "5G NR",
            CellId = "310410_55555",
            SignalStrengthDbm = -78,
            Latitude = 37.7749,
            Longitude = -122.4194
        };

        var response = await _client.PostAsJsonAsync("/api/cellular/snapshots", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CellularSnapshotDto>();
        result.Should().NotBeNull();
        result!.CellId.Should().Be("310410_55555");
        result.SignalStrengthDbm.Should().Be(-78);
        result.SignalRating.Should().Be("Excellent");
    }
}

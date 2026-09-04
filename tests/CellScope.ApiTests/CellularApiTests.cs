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

    [Fact]
    public async Task GetTowers_ReturnsTowerLocationsWithConnectedDevices()
    {
        var response = await _client.GetAsync("/api/towers?lat=37.7749&lon=-122.4194&radiusMeters=5000");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var towers = await response.Content.ReadFromJsonAsync<List<TowerLocationDto>>();
        towers.Should().NotBeNull();
        towers.Should().NotBeEmpty();
        towers![0].ConnectedDevices.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTowerConnectedDevices_ReturnsDeviceList()
    {
        var response = await _client.GetAsync("/api/towers/310410_12345/devices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var devices = await response.Content.ReadFromJsonAsync<List<TowerConnectedDeviceDto>>();
        devices.Should().NotBeNull();
        devices.Should().NotBeEmpty();
        devices![0].DeviceName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetTowerCalls_ReturnsActiveCallSessions()
    {
        var response = await _client.GetAsync("/api/towers/310410_12345/calls");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var calls = await response.Content.ReadFromJsonAsync<List<ActiveCallSessionDto>>();
        calls.Should().NotBeNull();
        calls.Should().NotBeEmpty();
        calls.Should().HaveCountGreaterThanOrEqualTo(10);
        calls![0].CallerNumber.Should().NotBeNullOrWhiteSpace();
        calls![0].ReceiverNumber.Should().NotBeNullOrWhiteSpace();
        calls![0].CallType.Should().NotBeNullOrWhiteSpace();
    }
}



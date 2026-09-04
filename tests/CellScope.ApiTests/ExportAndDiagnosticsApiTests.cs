using System.Net;
using System.Net.Http.Json;
using CellScope.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace CellScope.ApiTests;

public class ExportAndDiagnosticsApiTests : IClassFixture<CellScopeTestFactory>
{
    private readonly HttpClient _client;

    public ExportAndDiagnosticsApiTests(CellScopeTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDiagnostics_ReturnsHealthyDiagnostics()
    {
        var response = await _client.GetAsync("/api/diagnostics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var diag = await response.Content.ReadFromJsonAsync<SystemDiagnosticsDto>();
        diag.Should().NotBeNull();
        diag!.ApiStatus.Should().Be("Healthy");
        diag.DatabaseStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task ExportCsv_ReturnsCsvFile()
    {
        var response = await _client.GetAsync("/api/export/csv?type=everything");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportJson_ReturnsJsonFile()
    {
        var response = await _client.GetAsync("/api/export/json?type=everything");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetNetworkDevices_ReturnsDiscoveredInventory()
    {
        var response = await _client.GetAsync("/api/network/devices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var network = await response.Content.ReadFromJsonAsync<LocalNetworkDto>();
        network.Should().NotBeNull();
        network!.Devices.Should().NotBeEmpty();
        network.TotalDevices.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ToggleNetworkDevice_ChangesConnectionState()
    {
        var listResponse = await _client.GetAsync("/api/network/devices");
        var network = await listResponse.Content.ReadFromJsonAsync<LocalNetworkDto>();
        var firstDev = network!.Devices.First();

        var toggleResponse = await _client.PostAsync($"/api/network/devices/{firstDev.Id}/toggle", null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await toggleResponse.Content.ReadFromJsonAsync<NetworkDeviceDto>();
        updated.Should().NotBeNull();
        updated!.IsOnline.Should().Be(!firstDev.IsOnline);
    }

    [Fact]
    public async Task BulkConnectDisconnect_UpdatesAllDevices()
    {
        var disResponse = await _client.PostAsync("/api/network/disconnect-all", null);
        disResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var disNet = await disResponse.Content.ReadFromJsonAsync<LocalNetworkDto>();
        disNet!.Devices.All(d => !d.IsOnline).Should().BeTrue();

        var conResponse = await _client.PostAsync("/api/network/connect-all", null);
        conResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var conNet = await conResponse.Content.ReadFromJsonAsync<LocalNetworkDto>();
        conNet!.Devices.All(d => d.IsOnline).Should().BeTrue();
    }
}


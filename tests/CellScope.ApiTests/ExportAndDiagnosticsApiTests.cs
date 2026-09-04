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
}

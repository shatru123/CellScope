using System.Net;
using FluentAssertions;
using Xunit;

namespace CellScope.ApiTests;

public class HealthApiTests : IClassFixture<CellScopeTestFactory>
{
    private readonly HttpClient _client;

    public HealthApiTests(CellScopeTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReady_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

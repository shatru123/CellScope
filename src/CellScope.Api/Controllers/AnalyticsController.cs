using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IDemoDataService _demoDataService;

    public AnalyticsController(IAnalyticsService analyticsService, IDemoDataService demoDataService)
    {
        _analyticsService = analyticsService;
        _demoDataService = demoDataService;
    }

    [HttpGet("signal")]
    public async Task<ActionResult<SignalAnalyticsDto>> GetSignalAnalytics(
        [FromQuery] string timeRange = "24h",
        [FromQuery] string? operatorName = null,
        [FromQuery] string? technology = null,
        CancellationToken cancellationToken = default)
    {
        var analytics = await _analyticsService.GetAnalyticsAsync(timeRange, operatorName, technology, cancellationToken);
        if (analytics.TotalObservations == 0 && _demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GetDemoAnalytics(timeRange));
        }
        return Ok(analytics);
    }
}

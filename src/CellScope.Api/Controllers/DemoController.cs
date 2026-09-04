using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly INotificationPublisher _notifier;

    public DemoController(IDemoDataService demoDataService, INotificationPublisher notifier)
    {
        _demoDataService = demoDataService;
        _notifier = notifier;
    }

    [HttpGet("state")]
    public IActionResult GetState()
    {
        return Ok(new { isDemoActive = _demoDataService.IsDemoModeActive });
    }

    [HttpPost("toggle")]
    public IActionResult ToggleDemo([FromQuery] bool? active)
    {
        _demoDataService.IsDemoModeActive = active ?? !_demoDataService.IsDemoModeActive;
        if (_demoDataService.IsDemoModeActive)
        {
            _demoDataService.InitializeDemoState();
        }
        return Ok(new { isDemoActive = _demoDataService.IsDemoModeActive });
    }

    [HttpPost("tick")]
    public async Task<ActionResult<CellularSnapshotDto>> Tick(CancellationToken cancellationToken)
    {
        var snapshot = _demoDataService.GenerateNextTick();
        await _notifier.PublishSnapshotAsync(snapshot, cancellationToken);
        return Ok(snapshot);
    }
}

using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ICellularService _cellularService;
    private readonly IDemoDataService _demoDataService;

    public LocationsController(ICellularService cellularService, IDemoDataService demoDataService)
    {
        _cellularService = cellularService;
        _demoDataService = demoDataService;
    }

    [HttpGet("trail")]
    public async Task<ActionResult<IReadOnlyList<LocationPointDto>>> GetLocationTrail(
        [FromQuery] Guid? deviceId, [FromQuery] int limit = 200, CancellationToken cancellationToken = default)
    {
        var trail = await _cellularService.GetLocationTrailAsync(deviceId, limit, cancellationToken);
        if (trail.Count == 0 && _demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GetDemoTrail());
        }
        return Ok(trail);
    }
}

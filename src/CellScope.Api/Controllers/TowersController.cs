using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TowersController : ControllerBase
{
    private readonly ITowerService _towerService;
    private readonly IDemoDataService _demoDataService;

    public TowersController(ITowerService towerService, IDemoDataService demoDataService)
    {
        _towerService = towerService;
        _demoDataService = demoDataService;
    }

    [HttpGet("nearby")]
    public async Task<ActionResult<IReadOnlyList<TowerLocationDto>>> GetNearbyTowers(
        [FromQuery] double latitude = 37.7749,
        [FromQuery] double longitude = -122.4194,
        [FromQuery] double radiusMeters = 5000,
        CancellationToken cancellationToken = default)
    {
        var towers = await _towerService.GetNearbyTowersAsync(latitude, longitude, radiusMeters, cancellationToken);
        if (towers.Count == 0 && _demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GetDemoTowers());
        }
        return Ok(towers);
    }

    [HttpGet("{cellId}")]
    public async Task<ActionResult<TowerLocationDto>> GetTowerByCell(string cellId, [FromQuery] string? radioTech, CancellationToken cancellationToken)
    {
        var tower = await _towerService.GetTowerForCellAsync(cellId, radioTech, cancellationToken);
        if (tower == null)
            return NotFound(new { message = $"Tower location unavailable for Cell ID {cellId}." });

        return Ok(tower);
    }
}

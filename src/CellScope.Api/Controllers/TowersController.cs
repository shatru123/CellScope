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

    [HttpGet("")]
    [HttpGet("nearby")]
    public async Task<ActionResult<IReadOnlyList<TowerLocationDto>>> GetNearbyTowers(
        [FromQuery] double latitude = 37.7749,
        [FromQuery] double longitude = -122.4194,
        [FromQuery] double radiusMeters = 5000,
        [FromQuery] double? lat = null,
        [FromQuery] double? lon = null,
        CancellationToken cancellationToken = default)
    {
        var finalLat = lat ?? latitude;
        var finalLon = lon ?? longitude;
        var towers = await _towerService.GetNearbyTowersAsync(finalLat, finalLon, radiusMeters, cancellationToken);
        if (towers.Count == 0 && _demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GetDemoTowers(finalLat, finalLon, radiusMeters));
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

    [HttpGet("{cellId}/devices")]
    public async Task<ActionResult<IReadOnlyList<TowerConnectedDeviceDto>>> GetTowerDevices(string cellId, CancellationToken cancellationToken)
    {
        if (_demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GetDemoConnectedDevicesForTower(cellId));
        }

        var devices = await _towerService.GetConnectedDevicesForTowerAsync(cellId, cancellationToken);
        return Ok(devices);
    }

    [HttpGet("{cellId}/calls")]
    public async Task<ActionResult<IReadOnlyList<ActiveCallSessionDto>>> GetTowerCalls(string cellId, CancellationToken cancellationToken)
    {
        if (_demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GetDemoActiveCallsForTower(cellId));
        }

        var calls = await _towerService.GetActiveCallsForTowerAsync(cellId, cancellationToken);
        return Ok(calls);
    }
}


using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/radio")]
public class RadioAnalysisController : ControllerBase
{
    private readonly ICellularRadioAnalysisService _radioService;
    private readonly IPrivate5gCoreService _private5gService;
    private readonly ICellularService _cellularService;
    private readonly ITowerService _towerService;

    public RadioAnalysisController(
        ICellularRadioAnalysisService radioService,
        IPrivate5gCoreService private5gService,
        ICellularService cellularService,
        ITowerService towerService)
    {
        _radioService = radioService;
        _private5gService = private5gService;
        _cellularService = cellularService;
        _towerService = towerService;
    }

    [HttpGet("load")]
    public async Task<ActionResult<CellCapacityDto>> GetCellLoad(CancellationToken cancellationToken)
    {
        var snapshot = await _cellularService.GetCurrentSnapshotAsync(null, cancellationToken);
        var tower = snapshot != null && !string.IsNullOrEmpty(snapshot.CellId)
            ? await _towerService.GetTowerForCellAsync(snapshot.CellId, snapshot.RadioTechnology, cancellationToken)
            : null;

        var load = _radioService.CalculateCellLoad(snapshot, tower);
        return Ok(load);
    }

    [HttpGet("threat-audit")]
    public async Task<ActionResult<CellThreatAnalysisDto>> GetThreatAudit(CancellationToken cancellationToken)
    {
        var snapshot = await _cellularService.GetCurrentSnapshotAsync(null, cancellationToken);
        var neighbors = await _cellularService.GetCurrentNeighborsAsync(null, cancellationToken);
        var tower = snapshot != null && !string.IsNullOrEmpty(snapshot.CellId)
            ? await _towerService.GetTowerForCellAsync(snapshot.CellId, snapshot.RadioTechnology, cancellationToken)
            : null;

        var threat = _radioService.AnalyzeCellThreats(snapshot, tower, neighbors);
        return Ok(threat);
    }

    [HttpGet("sib")]
    public async Task<ActionResult<SibAnalysisDto>> GetSibBroadcast(CancellationToken cancellationToken)
    {
        var snapshot = await _cellularService.GetCurrentSnapshotAsync(null, cancellationToken);
        var tower = snapshot != null && !string.IsNullOrEmpty(snapshot.CellId)
            ? await _towerService.GetTowerForCellAsync(snapshot.CellId, snapshot.RadioTechnology, cancellationToken)
            : null;

        var sib = _radioService.DecodeSibAndChannel(snapshot, tower);
        return Ok(sib);
    }

    [HttpGet("propagation")]
    public async Task<ActionResult<RfPropagationModelDto>> GetRfPropagation([FromQuery] string? cellId, CancellationToken cancellationToken)
    {
        TowerLocationDto? tower = null;
        if (!string.IsNullOrEmpty(cellId))
        {
            tower = await _towerService.GetTowerForCellAsync(cellId, null, cancellationToken);
        }

        if (tower == null)
        {
            var nearby = await _towerService.GetNearbyTowersAsync(37.7749, -122.4194, 5000, cancellationToken);
            tower = nearby.FirstOrDefault() ?? new TowerLocationDto
            {
                CellId = "410-01-382910",
                RadioTechnology = "5G NR",
                Latitude = 37.7749,
                Longitude = -122.4194,
                RangeMeters = 2400
            };
        }

        var prop = _radioService.CalculateRfPropagation(tower);
        return Ok(prop);
    }

    [HttpGet("private5g/status")]
    public async Task<ActionResult<Private5gCoreStatusDto>> GetPrivate5gStatus(CancellationToken cancellationToken)
    {
        var status = await _private5gService.GetCoreStatusAsync(null, cancellationToken);
        return Ok(status);
    }

    [HttpGet("private5g/subscribers")]
    public async Task<ActionResult<IReadOnlyList<Private5gSubscriberDto>>> GetPrivate5gSubscribers(CancellationToken cancellationToken)
    {
        var subs = await _private5gService.GetConnectedSubscribersAsync(null, cancellationToken);
        return Ok(subs);
    }
}

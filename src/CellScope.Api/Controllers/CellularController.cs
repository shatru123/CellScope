using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CellularController : ControllerBase
{
    private readonly ICellularService _cellularService;
    private readonly IDemoDataService _demoDataService;

    public CellularController(ICellularService cellularService, IDemoDataService demoDataService)
    {
        _cellularService = cellularService;
        _demoDataService = demoDataService;
    }

    [HttpPost("snapshots")]
    public async Task<ActionResult<CellularSnapshotDto>> IngestSnapshot([FromBody] IngestSnapshotRequest request, CancellationToken cancellationToken)
    {
        if (request.DeviceId == Guid.Empty)
            request.DeviceId = Guid.NewGuid();

        var result = await _cellularService.IngestSnapshotAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("current")]
    public async Task<ActionResult<CellularSnapshotDto>> GetCurrentSnapshot([FromQuery] Guid? deviceId, CancellationToken cancellationToken)
    {
        var snapshot = await _cellularService.GetCurrentSnapshotAsync(deviceId, cancellationToken);
        if (snapshot == null && _demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GenerateNextTick());
        }

        if (snapshot == null)
            return NotFound(new { message = "No cellular snapshot available yet. Connect a collector device or enable Demo Mode." });

        return Ok(snapshot);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<CellularSnapshotDto>>> GetHistory([FromQuery] Guid? deviceId, [FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var history = await _cellularService.GetHistoryAsync(deviceId, limit, cancellationToken);
        return Ok(history);
    }

    [HttpGet("neighbors")]
    public async Task<ActionResult<IReadOnlyList<NeighborCellDto>>> GetNeighbors([FromQuery] Guid? deviceId, CancellationToken cancellationToken)
    {
        var neighbors = await _cellularService.GetCurrentNeighborsAsync(deviceId, cancellationToken);
        if (neighbors.Count == 0 && _demoDataService.IsDemoModeActive)
        {
            var demoCurrent = _demoDataService.GenerateNextTick();
            return Ok(demoCurrent.NeighborCells);
        }
        return Ok(neighbors);
    }

    [HttpGet("handovers")]
    public async Task<ActionResult<IReadOnlyList<CellHandoverDto>>> GetHandovers([FromQuery] Guid? deviceId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var handovers = await _cellularService.GetHandoversAsync(deviceId, limit, cancellationToken);
        if (handovers.Count == 0 && _demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GetDemoHandovers());
        }
        return Ok(handovers);
    }
}

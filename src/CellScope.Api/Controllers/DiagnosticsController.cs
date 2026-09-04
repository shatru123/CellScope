using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly IDiagnosticsService _diagnosticsService;

    public DiagnosticsController(IDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService;
    }

    [HttpGet]
    public async Task<ActionResult<SystemDiagnosticsDto>> RunDiagnostics(CancellationToken cancellationToken)
    {
        var result = await _diagnosticsService.RunDiagnosticsAsync(cancellationToken);
        return Ok(result);
    }
}

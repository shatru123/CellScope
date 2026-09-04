using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IAuthService _authService;

    public SettingsController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<UserSettingsDto>> GetSettings([FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var id = userId ?? Guid.Empty;
        var settings = await _authService.GetSettingsAsync(id, cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<UserSettingsDto>> UpdateSettings([FromQuery] Guid? userId, [FromBody] UserSettingsDto dto, CancellationToken cancellationToken)
    {
        var id = userId ?? Guid.Empty;
        var updated = await _authService.UpdateSettingsAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("purge")]
    public async Task<IActionResult> PurgeData([FromQuery] Guid? userId, [FromQuery] string target = "all", CancellationToken cancellationToken = default)
    {
        var id = userId ?? Guid.Empty;
        bool purged = await _authService.PurgeTelemetryDataAsync(id, target, cancellationToken);
        return Ok(new { success = purged, message = $"Successfully purged {target} data." });
    }
}

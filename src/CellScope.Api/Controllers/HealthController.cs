using CellScope.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly CellScopeDbContext _dbContext;

    public HealthController(CellScopeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "Healthy", timestamp = DateTimeOffset.UtcNow });
    }

    [HttpGet("health/ready")]
    public async Task<IActionResult> GetReady(CancellationToken cancellationToken)
    {
        bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return StatusCode(503, new { status = "Degraded", error = "Database connection failed" });
        }

        return Ok(new { status = "Ready", database = "Connected", timestamp = DateTimeOffset.UtcNow });
    }
}

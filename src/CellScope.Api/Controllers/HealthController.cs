using CellScope.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private static readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;
    private readonly CellScopeDbContext _dbContext;

    public HealthController(CellScopeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("health")]
    [HttpGet("healthz")]
    [HttpGet("api/health")]
    [HttpHead("health")]
    [HttpHead("healthz")]
    [HttpHead("api/health")]
    public IActionResult GetHealth()
    {
        var uptime = DateTimeOffset.UtcNow - StartTime;
        return Ok(new
        {
            status = "Healthy",
            service = "CellScope Carrier Cellular & Network Intelligence",
            version = "1.0.0",
            author = "Shatrughna Ambhore",
            uptime = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s",
            timestamp = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("ping")]
    [HttpGet("api/ping")]
    [HttpHead("ping")]
    [HttpHead("api/ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            status = "pong",
            message = "CellScope is active and responding.",
            timestamp = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("keepalive")]
    [HttpGet("api/keepalive")]
    [HttpGet("cron/keepalive")]
    [HttpHead("keepalive")]
    [HttpHead("api/keepalive")]
    [HttpHead("cron/keepalive")]
    public async Task<IActionResult> KeepAlive(CancellationToken cancellationToken)
    {
        bool dbConnected = false;
        try
        {
            dbConnected = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch { }

        var uptime = DateTimeOffset.UtcNow - StartTime;

        return Ok(new
        {
            status = "Active",
            cronKeepAlive = true,
            message = "Render free tier instance kept awake successfully.",
            database = dbConnected ? "Connected" : "Degraded",
            provider = _dbContext.Database.ProviderName,
            uptime = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s",
            timestamp = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("health/ready")]
    public async Task<IActionResult> GetReady(CancellationToken cancellationToken)
    {
        bool canConnect = false;
        try
        {
            canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch { }

        if (!canConnect)
        {
            return StatusCode(503, new { status = "Degraded", error = "Database connection failed", timestamp = DateTimeOffset.UtcNow });
        }

        return Ok(new { status = "Ready", database = "Connected", timestamp = DateTimeOffset.UtcNow });
    }
}

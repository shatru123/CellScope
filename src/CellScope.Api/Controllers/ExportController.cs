using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet("csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string type = "everything", CancellationToken cancellationToken = default)
    {
        string csv = await _exportService.ExportAsCsvAsync(type, cancellationToken);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"cellscope_export_{type}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    [HttpGet("json")]
    public async Task<IActionResult> ExportJson([FromQuery] string type = "everything", CancellationToken cancellationToken = default)
    {
        string json = await _exportService.ExportAsJsonAsync(type, cancellationToken);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"cellscope_export_{type}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
    }
}

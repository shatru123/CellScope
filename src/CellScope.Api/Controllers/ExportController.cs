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

    [HttpGet("kml")]
    public async Task<IActionResult> ExportKml([FromServices] ITowerService towerService, [FromServices] ICellularService cellService, [FromServices] IGisExportService gisService, CancellationToken cancellationToken = default)
    {
        var towers = await towerService.GetNearbyTowersAsync(18.5204, 73.8567, 50000, cancellationToken);
        var snapshot = await cellService.GetCurrentSnapshotAsync(cancellationToken: cancellationToken);
        var trail = await cellService.GetLocationTrailAsync(cancellationToken: cancellationToken);
        string kml = gisService.GenerateKml(towers, snapshot, trail);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(kml);
        return File(bytes, "application/vnd.google-earth.kml+xml", $"cellscope_export_3d_{DateTime.UtcNow:yyyyMMdd_HHmmss}.kml");
    }

    [HttpGet("geojson")]
    public async Task<IActionResult> ExportGeoJson([FromServices] ITowerService towerService, [FromServices] ICellularService cellService, [FromServices] IGisExportService gisService, CancellationToken cancellationToken = default)
    {
        var towers = await towerService.GetNearbyTowersAsync(18.5204, 73.8567, 50000, cancellationToken);
        var snapshot = await cellService.GetCurrentSnapshotAsync(cancellationToken: cancellationToken);
        var trail = await cellService.GetLocationTrailAsync(cancellationToken: cancellationToken);
        string geojson = gisService.GenerateGeoJson(towers, snapshot, trail);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(geojson);
        return File(bytes, "application/geo+json", $"cellscope_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.geojson");
    }
}

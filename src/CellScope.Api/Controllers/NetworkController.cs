using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NetworkController : ControllerBase
{
    private readonly ILocalNetworkService _networkService;
    private readonly IDemoDataService _demoDataService;

    public NetworkController(ILocalNetworkService networkService, IDemoDataService demoDataService)
    {
        _networkService = networkService;
        _demoDataService = demoDataService;
    }

    [HttpPost("scan")]
    public async Task<ActionResult<LocalNetworkDto>> ScanNetwork([FromQuery] string? subnet, CancellationToken cancellationToken)
    {
        var result = await _networkService.ScanLocalSubnetAsync(subnet, cancellationToken);
        return Ok(result);
    }

    [HttpGet("devices")]
    public async Task<ActionResult<LocalNetworkDto>> GetDevices(CancellationToken cancellationToken)
    {
        var latest = await _networkService.GetLatestNetworkScanAsync(cancellationToken);
        if (latest == null && _demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.GetDemoLocalNetwork());
        }

        if (latest == null)
            return Ok(new LocalNetworkDto { Subnet = "Not Scanned", TotalDevices = 0 });

        return Ok(latest);
    }

    [HttpGet("devices/{id:guid}")]
    public async Task<ActionResult<NetworkDeviceDto>> GetDeviceById(Guid id, CancellationToken cancellationToken)
    {
        var dev = await _networkService.GetDeviceByIdAsync(id, cancellationToken);
        if (dev == null)
            return NotFound(new { message = "Device not found." });

        return Ok(dev);
    }
}

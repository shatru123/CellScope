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
        if (_demoDataService.IsDemoModeActive)
        {
            var demoDev = _demoDataService.GetDemoLocalNetwork().Devices.FirstOrDefault(d => d.Id == id);
            if (demoDev != null) return Ok(demoDev);
        }

        var dev = await _networkService.GetDeviceByIdAsync(id, cancellationToken);
        if (dev == null)
            return NotFound(new { message = "Device not found." });

        return Ok(dev);
    }

    [HttpPost("devices/{id:guid}/toggle")]
    public async Task<ActionResult<NetworkDeviceDto>> ToggleDevice(Guid id, CancellationToken cancellationToken)
    {
        if (_demoDataService.IsDemoModeActive)
        {
            var res = _demoDataService.ToggleDemoDeviceConnection(id);
            if (res != null) return Ok(res);
        }

        var updated = await _networkService.ToggleDeviceConnectionAsync(id, cancellationToken);
        if (updated == null)
            return NotFound(new { message = "Device not found." });

        return Ok(updated);
    }

    [HttpPost("devices/{id:guid}/connect")]
    public async Task<ActionResult<NetworkDeviceDto>> ConnectDevice(Guid id, CancellationToken cancellationToken)
    {
        if (_demoDataService.IsDemoModeActive)
        {
            var res = _demoDataService.SetDemoDeviceConnection(id, true);
            if (res != null) return Ok(res);
        }

        var updated = await _networkService.SetDeviceConnectionAsync(id, true, cancellationToken);
        if (updated == null)
            return NotFound(new { message = "Device not found." });

        return Ok(updated);
    }

    [HttpPost("devices/{id:guid}/disconnect")]
    public async Task<ActionResult<NetworkDeviceDto>> DisconnectDevice(Guid id, CancellationToken cancellationToken)
    {
        if (_demoDataService.IsDemoModeActive)
        {
            var res = _demoDataService.SetDemoDeviceConnection(id, false);
            if (res != null) return Ok(res);
        }

        var updated = await _networkService.SetDeviceConnectionAsync(id, false, cancellationToken);
        if (updated == null)
            return NotFound(new { message = "Device not found." });

        return Ok(updated);
    }

    [HttpPost("connect-all")]
    public async Task<ActionResult<LocalNetworkDto>> ConnectAll(CancellationToken cancellationToken)
    {
        if (_demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.SetAllDemoDevicesConnection(true));
        }

        var net = await _networkService.SetAllDevicesConnectionAsync(true, cancellationToken);
        return Ok(net);
    }

    [HttpPost("disconnect-all")]
    public async Task<ActionResult<LocalNetworkDto>> DisconnectAll(CancellationToken cancellationToken)
    {
        if (_demoDataService.IsDemoModeActive)
        {
            return Ok(_demoDataService.SetAllDemoDevicesConnection(false));
        }

        var net = await _networkService.SetAllDevicesConnectionAsync(false, cancellationToken);
        return Ok(net);
    }

    [HttpPost("toggle-adapter")]
    public async Task<ActionResult<LocalNetworkDto>> ToggleAdapter(CancellationToken cancellationToken)
    {
        if (_demoDataService.IsDemoModeActive)
        {
            _demoDataService.ToggleDemoAdapter();
            return Ok(_demoDataService.GetDemoLocalNetwork());
        }

        var net = await _networkService.ToggleAdapterConnectionAsync(cancellationToken);
        return Ok(net);
    }
}


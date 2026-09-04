using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CellScope.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<DeviceDto>> RegisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var result = await _deviceService.RegisterDeviceAsync(request, null, cancellationToken);
        return Ok(result);
    }

    [HttpPost("pair/request")]
    public async Task<ActionResult<object>> RequestPairingCode([FromQuery] Guid deviceId, CancellationToken cancellationToken)
    {
        var code = await _deviceService.GeneratePairingCodeAsync(deviceId, cancellationToken);
        return Ok(new { pairingCode = code });
    }

    [HttpPost("pair/confirm")]
    public async Task<ActionResult<PairDeviceResponse>> ConfirmPairing([FromBody] PairDeviceRequest request, CancellationToken cancellationToken)
    {
        var response = await _deviceService.PairDeviceAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceDto>>> GetDevices([FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var list = await _deviceService.GetDevicesAsync(userId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeviceDto>> GetDevice(Guid id, CancellationToken cancellationToken)
    {
        var device = await _deviceService.GetDeviceByIdAsync(id, cancellationToken);
        if (device == null)
            return NotFound(new { message = "Device not found." });

        return Ok(device);
    }

    [HttpPost("{id:guid}/heartbeat")]
    public async Task<IActionResult> Heartbeat(Guid id, CancellationToken cancellationToken)
    {
        bool success = await _deviceService.UpdateHeartbeatAsync(id, cancellationToken);
        return success ? Ok(new { status = "Heartbeat updated" }) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDevice(Guid id, CancellationToken cancellationToken)
    {
        bool deleted = await _deviceService.DeleteDeviceAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

using CellScope.Api.Hubs;
using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CellScope.Api.Services;

public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NetworkHub> _hubContext;

    public SignalRNotificationPublisher(IHubContext<NetworkHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishSnapshotAsync(CellularSnapshotDto snapshot, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveSnapshotUpdate", snapshot, cancellationToken);
    }

    public async Task PublishHandoverAsync(CellHandoverDto handover, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveHandoverEvent", handover, cancellationToken);
    }

    public async Task PublishDeviceStatusAsync(DeviceDto device, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveDeviceStatus", device, cancellationToken);
    }

    public async Task PublishNetworkScanAsync(LocalNetworkDto network, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveScanUpdate", network, cancellationToken);
    }
}

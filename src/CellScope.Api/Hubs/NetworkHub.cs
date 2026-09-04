using Microsoft.AspNetCore.SignalR;

namespace CellScope.Api.Hubs;

public class NetworkHub : Hub
{
    private static int _connectedClients = 0;

    public static int ConnectedClientsCount => Math.Max(0, _connectedClients);

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectedClients);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _connectedClients);
        await base.OnDisconnectedAsync(exception);
    }
}

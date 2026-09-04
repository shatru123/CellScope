using System.Net.Http.Json;
using CellScope.Application.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace CellScope.Desktop.Services;

public class CellScopeApiClient
{
    private readonly HttpClient _httpClient;
    private HubConnection? _hubConnection;

    public event Action<CellularSnapshotDto>? OnSnapshotReceived;
    public event Action<CellHandoverDto>? OnHandoverReceived;
    public event Action<LocalNetworkDto>? OnNetworkScanReceived;

    public CellScopeApiClient(string baseUrl = "http://localhost:5000")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<bool> InitializeSignalRAsync(string hubUrl = "http://localhost:5000/hubs/network")
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<CellularSnapshotDto>("ReceiveSnapshotUpdate", snapshot =>
            {
                OnSnapshotReceived?.Invoke(snapshot);
            });

            _hubConnection.On<CellHandoverDto>("ReceiveHandoverEvent", handover =>
            {
                OnHandoverReceived?.Invoke(handover);
            });

            _hubConnection.On<LocalNetworkDto>("ReceiveScanUpdate", network =>
            {
                OnNetworkScanReceived?.Invoke(network);
            });

            await _hubConnection.StartAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<CellularSnapshotDto?> GetCurrentSnapshotAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CellularSnapshotDto>("/api/cellular/current");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TowerLocationDto>> GetNearbyTowersAsync(double lat, double lon)
    {
        try
        {
            var towers = await _httpClient.GetFromJsonAsync<List<TowerLocationDto>>($"/api/towers/nearby?latitude={lat}&longitude={lon}");
            return towers ?? new List<TowerLocationDto>();
        }
        catch
        {
            return new List<TowerLocationDto>();
        }
    }

    public async Task<LocalNetworkDto?> ScanLocalNetworkAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/network/scan", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LocalNetworkDto>();
            }
        }
        catch { }
        return null;
    }
}

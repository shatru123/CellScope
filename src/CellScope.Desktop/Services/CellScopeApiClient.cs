using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CellScope.Application.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace CellScope.Desktop.Services;

public class CellScopeApiClient
{
    private HttpClient _httpClient;
    private HubConnection? _hubConnection;
    public string BaseUrl { get; private set; }

    public event Action<CellularSnapshotDto>? OnSnapshotReceived;
    public event Action<CellHandoverDto>? OnHandoverReceived;
    public event Action<LocalNetworkDto>? OnNetworkScanReceived;

    public CellScopeApiClient(string baseUrl = "http://localhost:5050")
    {
        BaseUrl = baseUrl;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<bool> DetectAndConnectAsync()
    {
        var candidates = new[] { "http://localhost:5050", "http://localhost:5000", "http://127.0.0.1:5050", "http://127.0.0.1:5000" };
        foreach (var url in candidates)
        {
            try
            {
                using var testClient = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(2) };
                var res = await testClient.GetAsync("/health");
                if (!res.IsSuccessStatusCode)
                {
                    res = await testClient.GetAsync("/api/health");
                }
                if (res.IsSuccessStatusCode)
                {
                    BaseUrl = url;
                    _httpClient = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(5) };
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> InitializeSignalRAsync()
    {
        try
        {
            string hubUrl = $"{BaseUrl.TrimEnd('/')}/hubs/network";
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

    public async Task<IReadOnlyList<TowerLocationDto>> GetNearbyTowersAsync(double lat = 37.7749, double lon = -122.4194)
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

    public async Task<IReadOnlyList<TowerConnectedDeviceDto>> GetTowerDevicesAsync(string cellId)
    {
        try
        {
            var devices = await _httpClient.GetFromJsonAsync<List<TowerConnectedDeviceDto>>($"/api/towers/{cellId}/devices");
            return devices ?? new List<TowerConnectedDeviceDto>();
        }
        catch
        {
            return new List<TowerConnectedDeviceDto>();
        }
    }

    public async Task<IReadOnlyList<ActiveCallSessionDto>> GetTowerCallsAsync(string cellId)
    {
        try
        {
            var calls = await _httpClient.GetFromJsonAsync<List<ActiveCallSessionDto>>($"/api/towers/{cellId}/calls");
            return calls ?? new List<ActiveCallSessionDto>();
        }
        catch
        {
            return new List<ActiveCallSessionDto>();
        }
    }

    public async Task<LocalNetworkDto?> GetLocalNetworkAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<LocalNetworkDto>("/api/network/latest");
        }
        catch
        {
            return null;
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

    public async Task<NetworkDeviceDto?> ToggleDeviceConnectionAsync(Guid deviceId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/network/devices/{deviceId}/toggle", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NetworkDeviceDto>();
            }
        }
        catch { }
        return null;
    }

    public async Task<LocalNetworkDto?> SetAllDevicesConnectionAsync(bool isConnect)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/network/devices/bulk?isOnline={isConnect}", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LocalNetworkDto>();
            }
        }
        catch { }
        return null;
    }

    public async Task<IReadOnlyList<DeviceDto>> GetRegisteredDevicesAsync()
    {
        try
        {
            var list = await _httpClient.GetFromJsonAsync<List<DeviceDto>>("/api/devices");
            return list ?? new List<DeviceDto>();
        }
        catch
        {
            return new List<DeviceDto>();
        }
    }

    public async Task<SystemDiagnosticsDto?> GetDiagnosticsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SystemDiagnosticsDto>("/api/diagnostics");
        }
        catch
        {
            return null;
        }
    }
}

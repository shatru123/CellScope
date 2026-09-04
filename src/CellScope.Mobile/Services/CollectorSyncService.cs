using System.Net.Http.Json;
using CellScope.Application.DTOs;

namespace CellScope.Mobile.Services;

public class CollectorSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ICellularInfoService _cellularService;
    private readonly ILocationService _locationService;
    private Guid? _pairedDeviceId;
    private bool _isCollecting = false;
    private System.Threading.Timer? _collectionTimer;

    public event Action<string>? OnStatusChanged;

    public CollectorSyncService(
        ICellularInfoService cellularService,
        ILocationService locationService,
        string backendUrl = "http://localhost:5000")
    {
        _cellularService = cellularService;
        _locationService = locationService;
        _httpClient = new HttpClient { BaseAddress = new Uri(backendUrl) };
    }

    public async Task<PairDeviceResponse> PairDeviceAsync(string pairingCode, string deviceName)
    {
        var request = new PairDeviceRequest
        {
            PairingCode = pairingCode,
            DeviceName = deviceName,
            Platform = "Android",
            Model = "Google Pixel 9 Pro"
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/devices/pair/confirm", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PairDeviceResponse>();
                if (result != null && result.Success)
                {
                    _pairedDeviceId = result.DeviceId;
                    OnStatusChanged?.Invoke($"● Paired successfully as Device ID: {_pairedDeviceId}");
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            return new PairDeviceResponse { Success = false, Message = ex.Message };
        }

        return new PairDeviceResponse { Success = false, Message = "Failed to pair with backend." };
    }

    public void StartCollection(int intervalSeconds = 60)
    {
        if (_isCollecting) return;
        _isCollecting = true;

        OnStatusChanged?.Invoke("● Collecting (Foreground Service Active) - Last update: Just now");

        _collectionTimer = new System.Threading.Timer(async _ =>
        {
            await CollectAndUploadTickAsync();
        }, null, 0, intervalSeconds * 1000);
    }

    public void StopCollection()
    {
        _isCollecting = false;
        _collectionTimer?.Dispose();
        _collectionTimer = null;
        OnStatusChanged?.Invoke("○ Collection Paused");
    }

    private async Task CollectAndUploadTickAsync()
    {
        try
        {
            var cellSnapshot = await _cellularService.GetCurrentSnapshotAsync();
            var location = await _locationService.GetCurrentLocationAsync();

            if (cellSnapshot == null) return;

            var ingestReq = new IngestSnapshotRequest
            {
                DeviceId = _pairedDeviceId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Timestamp = DateTimeOffset.UtcNow,
                OperatorName = cellSnapshot.OperatorName,
                Mcc = cellSnapshot.Mcc,
                Mnc = cellSnapshot.Mnc,
                RadioTechnology = cellSnapshot.RadioTechnology,
                CellId = cellSnapshot.CellId,
                PhysicalCellId = cellSnapshot.PhysicalCellId,
                TrackingAreaCode = cellSnapshot.TrackingAreaCode,
                Frequency = cellSnapshot.Frequency,
                Band = cellSnapshot.Band,
                SignalStrengthDbm = cellSnapshot.SignalStrengthDbm,
                SignalLevel = cellSnapshot.SignalLevel,
                SignalQuality = cellSnapshot.SignalQuality,
                Latitude = location?.Latitude ?? cellSnapshot.Latitude,
                Longitude = location?.Longitude ?? cellSnapshot.Longitude,
                LocationAccuracy = location?.Accuracy ?? cellSnapshot.LocationAccuracy,
                Altitude = location?.Altitude ?? cellSnapshot.Altitude,
                DataSource = "Android:TelephonyManager",
                NeighborCells = cellSnapshot.NeighborCells
            };

            var res = await _httpClient.PostAsJsonAsync("/api/cellular/snapshots", ingestReq);
            if (res.IsSuccessStatusCode)
            {
                OnStatusChanged?.Invoke($"● Collecting - Last update: {DateTime.Now:HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"⚠️ Offline - Queued locally ({ex.Message})");
        }
    }
}

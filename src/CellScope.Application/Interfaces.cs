using CellScope.Application.DTOs;
using CellScope.Domain.Entities;

namespace CellScope.Application.Interfaces;

public interface ICellularService
{
    Task<CellularSnapshotDto> IngestSnapshotAsync(IngestSnapshotRequest request, CancellationToken cancellationToken = default);
    Task<CellularSnapshotDto?> GetCurrentSnapshotAsync(Guid? deviceId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CellularSnapshotDto>> GetHistoryAsync(Guid? deviceId = null, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NeighborCellDto>> GetCurrentNeighborsAsync(Guid? deviceId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CellHandoverDto>> GetHandoversAsync(Guid? deviceId = null, int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationPointDto>> GetLocationTrailAsync(Guid? deviceId = null, int limit = 200, CancellationToken cancellationToken = default);
}

public interface ITowerService
{
    Task<IReadOnlyList<TowerLocationDto>> GetNearbyTowersAsync(double latitude, double longitude, double radiusMeters = 5000, CancellationToken cancellationToken = default);
    Task<TowerLocationDto?> GetTowerForCellAsync(string cellId, string? radioTech = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TowerConnectedDeviceDto>> GetConnectedDevicesForTowerAsync(string cellId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActiveCallSessionDto>> GetActiveCallsForTowerAsync(string cellId, CancellationToken cancellationToken = default);
    Task SeedDefaultTowersAsync(CancellationToken cancellationToken = default);
}

public interface ILocalNetworkService
{
    Task<LocalNetworkDto> ScanLocalSubnetAsync(string? specificSubnet = null, CancellationToken cancellationToken = default);
    Task<LocalNetworkDto?> GetLatestNetworkScanAsync(CancellationToken cancellationToken = default);
    Task<NetworkDeviceDto?> GetDeviceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NetworkDeviceDto?> ToggleDeviceConnectionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NetworkDeviceDto?> SetDeviceConnectionAsync(Guid id, bool isConnected, CancellationToken cancellationToken = default);
    Task<LocalNetworkDto> SetAllDevicesConnectionAsync(bool isConnected, CancellationToken cancellationToken = default);
    Task<LocalNetworkDto> ToggleAdapterConnectionAsync(CancellationToken cancellationToken = default);
    string ResolveVendorFromMac(string macAddress);
}

public interface IAnalyticsService
{
    Task<SignalAnalyticsDto> GetAnalyticsAsync(string timeRange = "24h", string? operatorName = null, string? technology = null, CancellationToken cancellationToken = default);
}

public interface IDeviceService
{
    Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceRequest request, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<string> GeneratePairingCodeAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<PairDeviceResponse> PairDeviceAsync(PairDeviceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceDto>> GetDevicesAsync(Guid? userId = null, CancellationToken cancellationToken = default);
    Task<DeviceDto?> GetDeviceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UpdateHeartbeatAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<bool> DeleteDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
}

public interface IExportService
{
    Task<string> ExportAsCsvAsync(string dataType = "everything", CancellationToken cancellationToken = default);
    Task<string> ExportAsJsonAsync(string dataType = "everything", CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(AuthRequest request, CancellationToken cancellationToken = default);
    Task<UserSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSettingsDto> UpdateSettingsAsync(Guid userId, UserSettingsDto settings, CancellationToken cancellationToken = default);
    Task<bool> PurgeTelemetryDataAsync(Guid userId, string target = "all", CancellationToken cancellationToken = default);
}

public interface IDiagnosticsService
{
    Task<SystemDiagnosticsDto> RunDiagnosticsAsync(CancellationToken cancellationToken = default);
}

public interface IDemoDataService
{
    bool IsDemoModeActive { get; set; }
    bool IsDemoAdapterConnected { get; set; }
    event Action? OnModeChanged;
    void SetMode(bool isDemoMode);
    void InitializeDemoState();
    CellularSnapshotDto GenerateNextTick();
    IReadOnlyList<TowerLocationDto> GetDemoTowers(double? latitude = null, double? longitude = null, double radiusMeters = 8000);
    IReadOnlyList<TowerConnectedDeviceDto> GetDemoConnectedDevicesForTower(string cellId);
    IReadOnlyList<ActiveCallSessionDto> GetDemoActiveCallsForTower(string cellId);

    IReadOnlyList<LocationPointDto> GetDemoTrail();
    IReadOnlyList<CellHandoverDto> GetDemoHandovers();
    LocalNetworkDto GetDemoLocalNetwork();
    NetworkDeviceDto? ToggleDemoDeviceConnection(Guid id);
    NetworkDeviceDto? SetDemoDeviceConnection(Guid id, bool isConnected);
    LocalNetworkDto SetAllDemoDevicesConnection(bool isConnected);
    bool ToggleDemoAdapter();
    SignalAnalyticsDto GetDemoAnalytics(string timeRange);
}

public interface INotificationPublisher
{
    Task PublishSnapshotAsync(CellularSnapshotDto snapshot, CancellationToken cancellationToken = default);
    Task PublishHandoverAsync(CellHandoverDto handover, CancellationToken cancellationToken = default);
    Task PublishDeviceStatusAsync(DeviceDto device, CancellationToken cancellationToken = default);
    Task PublishNetworkScanAsync(LocalNetworkDto network, CancellationToken cancellationToken = default);
}

public interface ISecurityAnalysisService
{
    AesTraceResultDto TraceAes128(byte[] key, byte[] plaintext);
    MilenageResultDto ComputeMilenage(byte[] key, byte[] opOrOpc, bool isOpc, byte[] rand, byte[] sqn, byte[] amf);
    FiveGAkaResultDto Compute5GAka(byte[] key, byte[] opOrOpc, bool isOpc, byte[] rand, byte[] sqn, byte[] amf, string servingNetworkName, string supi);
    KdfCalculationDto ComputeKdf(byte[] key, byte fc, List<(byte[] Param, string Label)> parameters);
    IReadOnlyList<SecurityTestVectorDto> GetPredefinedTestVectors();
    IReadOnlyList<SecurityTestVectorDto> RunAllTestVectorVerifications();
    IReadOnlyList<AuthFlowMessageDto> GetAuthenticationFlowMessages();
}

public interface ICellularRadioAnalysisService
{
    CellCapacityDto CalculateCellLoad(CellularSnapshotDto? snapshot, TowerLocationDto? tower);
    CellThreatAnalysisDto AnalyzeCellThreats(CellularSnapshotDto? snapshot, TowerLocationDto? tower, IReadOnlyList<NeighborCellDto>? neighbors);
    SibAnalysisDto DecodeSibAndChannel(CellularSnapshotDto? snapshot, TowerLocationDto? tower);
    RfPropagationModelDto CalculateRfPropagation(TowerLocationDto tower, double carrierFrequencyMhz = 3500.0);
    IReadOnlyList<RfPropagationModelDto> GetMultiTowerPropagation(IReadOnlyList<TowerLocationDto> towers);
}

public interface IPrivate5gCoreService
{
    Task<Private5gCoreStatusDto> GetCoreStatusAsync(string? endpointUrl = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Private5gSubscriberDto>> GetConnectedSubscribersAsync(string? endpointUrl = null, CancellationToken cancellationToken = default);
    Task<Private5gSubscriberDto?> GetSubscriberBySupiAsync(string supi, CancellationToken cancellationToken = default);
}



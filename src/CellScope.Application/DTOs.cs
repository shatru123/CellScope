using CellScope.Domain.Enums;

namespace CellScope.Application.DTOs;

public class CellularSnapshotDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public string? OperatorName { get; set; }
    public int? Mcc { get; set; }
    public int? Mnc { get; set; }

    public string? RadioTechnology { get; set; }
    public string? CellId { get; set; }
    public string? TrackingAreaCode { get; set; }
    public string? PhysicalCellId { get; set; }

    public string? Frequency { get; set; }
    public string? Band { get; set; }

    public int? SignalStrengthDbm { get; set; }
    public int? SignalLevel { get; set; }
    public double? SignalQuality { get; set; }

    public string SignalRating { get; set; } = "Unavailable";
    public string SignalColor { get; set; } = "#6B7280";
    public int SignalPercentage { get; set; } = 0;

    public bool? IsRegistered { get; set; }
    public bool? IsRoaming { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? LocationAccuracy { get; set; }
    public double? Altitude { get; set; }

    public string? DataSource { get; set; }
    public List<NeighborCellDto> NeighborCells { get; set; } = new();
}

public class IngestSnapshotRequest
{
    public Guid DeviceId { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? OperatorName { get; set; }
    public int? Mcc { get; set; }
    public int? Mnc { get; set; }
    public string? RadioTechnology { get; set; }
    public string? CellId { get; set; }
    public string? TrackingAreaCode { get; set; }
    public string? PhysicalCellId { get; set; }
    public string? Frequency { get; set; }
    public string? Band { get; set; }
    public int? SignalStrengthDbm { get; set; }
    public int? SignalLevel { get; set; }
    public double? SignalQuality { get; set; }
    public bool? IsRegistered { get; set; }
    public bool? IsRoaming { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? LocationAccuracy { get; set; }
    public double? Altitude { get; set; }
    public string? DataSource { get; set; }
    public List<NeighborCellDto> NeighborCells { get; set; } = new();
}

public class NeighborCellDto
{
    public Guid Id { get; set; }
    public string? CellId { get; set; }
    public string? PhysicalCellId { get; set; }
    public string? TrackingAreaCode { get; set; }
    public string? RadioTechnology { get; set; }
    public string? Band { get; set; }
    public string? Frequency { get; set; }
    public int? SignalStrengthDbm { get; set; }
    public double? SignalQuality { get; set; }
    public string SignalRating { get; set; } = "Unavailable";
    public string SignalColor { get; set; } = "#6B7280";
    public bool? IsRegistered { get; set; }
}

public class TowerLocationDto
{
    public Guid Id { get; set; }
    public string CellId { get; set; } = string.Empty;
    public string? PhysicalCellId { get; set; }
    public string RadioTechnology { get; set; } = "LTE";
    public int Mcc { get; set; }
    public int Mnc { get; set; }
    public string? LacTac { get; set; }
    public string? OperatorName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? RangeMeters { get; set; }
    public int Samples { get; set; }
    public string Confidence { get; set; } = "Medium";
    public string Source { get; set; } = "Open/public dataset";
    public string? SourceReference { get; set; }
    public DateTimeOffset LastVerified { get; set; }
    public double DistanceMeters { get; set; }
    public List<TowerConnectedDeviceDto> ConnectedDevices { get; set; } = new();
}

public class TowerConnectedDeviceDto
{
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Mobile Collector";
    public string Platform { get; set; } = "Android";
    public string Model { get; set; } = "Collector Node";
    public string RadioTechnology { get; set; } = "5G NR";
    public string Band { get; set; } = "n78";
    public int SignalStrengthDbm { get; set; }
    public double? SignalQuality { get; set; }
    public string SignalRating { get; set; } = "Good";
    public string SignalColor { get; set; } = "#10B981";
    public int EstimatedDistanceMeters { get; set; }
    public int TimingAdvance { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public string ConnectionState { get; set; } = "Active Attached";
}

public class CellHandoverDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string PreviousCellId { get; set; } = string.Empty;
    public string NewCellId { get; set; } = string.Empty;
    public string? PreviousRadioTechnology { get; set; }
    public string? NewRadioTechnology { get; set; }
    public int? PreviousSignalDbm { get; set; }
    public int? NewSignalDbm { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? TriggerReason { get; set; }
}

public class LocationPointDto
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public double? Bearing { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class DeviceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = "Android";
    public string? Model { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? PairingCode { get; set; }
    public bool IsPaired { get; set; }
    public DateTimeOffset? PairedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsOnline => DateTimeOffset.UtcNow - LastSeenAt < TimeSpan.FromMinutes(3);
}

public class RegisterDeviceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = "Android";
    public string? Model { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
}

public class PairDeviceRequest
{
    public string PairingCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = "Android";
    public string? Model { get; set; }
}

public class PairDeviceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? DeviceId { get; set; }
    public string? DeviceToken { get; set; }
}

public class LocalNetworkDto
{
    public Guid Id { get; set; }
    public string Subnet { get; set; } = string.Empty;
    public string? GatewayIp { get; set; }
    public string? InterfaceName { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
    public int TotalDevices { get; set; }
    public bool IsAdapterConnected { get; set; } = true;
    public List<NetworkDeviceDto> Devices { get; set; } = new();

    public int ConnectedCount => Devices.Count(d => d.IsOnline);
    public int DisconnectedCount => Devices.Count(d => !d.IsOnline);
}

public class NetworkDeviceDto
{
    public Guid Id { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public string? Hostname { get; set; }
    public string? Vendor { get; set; }
    public string DeviceType { get; set; } = "Unknown";
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public long? ResponseTimeMs { get; set; }
    public bool IsOnline { get; set; } = true;
    public string? SafeServiceSummary { get; set; }
    public string ConnectionBand { get; set; } = "Wi-Fi 6 (5 GHz)";
    public int LinkSpeedMbps { get; set; } = 1200;
    public string IpAssignment { get; set; } = "DHCP Dynamic";
    public string ConnectionStatus => IsOnline ? "Connected (Online)" : "Disconnected (Blocked)";
}

public class SignalAnalyticsDto
{
    public List<TimeSeriesPoint<int>> SignalStrengthTrend { get; set; } = new();
    public List<TimeSeriesPoint<double>> SignalQualityTrend { get; set; } = new();
    public Dictionary<string, int> TechnologyDistribution { get; set; } = new();
    public Dictionary<string, double> OperatorAverageSignal { get; set; } = new();
    public Dictionary<string, int> RatingDistribution { get; set; } = new();
    public int TotalObservations { get; set; }
    public int TotalHandovers { get; set; }
    public double AverageSignalStrength { get; set; }
    public int MinSignalStrength { get; set; }
    public int MaxSignalStrength { get; set; }
}

public class TimeSeriesPoint<T>
{
    public DateTimeOffset Timestamp { get; set; }
    public T Value { get; set; } = default!;
    public string? Label { get; set; }
}

public class AuthRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public UserDto? User { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public class UserSettingsDto
{
    public bool LocationCollectionEnabled { get; set; } = true;
    public bool CellularCollectionEnabled { get; set; } = true;
    public bool LocalNetworkDiscoveryEnabled { get; set; } = true;
    public bool CloudSyncEnabled { get; set; } = true;
    public int DataRetentionDays { get; set; } = 90;
    public string Theme { get; set; } = "Dark";
    public int CollectionIntervalSeconds { get; set; } = 60;
    public bool BatterySavingMode { get; set; } = false;
}

public class SystemDiagnosticsDto
{
    public string ApiStatus { get; set; } = "Healthy";
    public string DatabaseStatus { get; set; } = "Healthy";
    public long DatabaseLatencyMs { get; set; }
    public string SignalRStatus { get; set; } = "Connected";
    public int ActiveConnections { get; set; }
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public DateTimeOffset? LastCellularUpdate { get; set; }
    public string LocationServiceStatus { get; set; } = "Available";
    public string PermissionsStatus { get; set; } = "All granted";
    public bool IsDemoMode { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
}

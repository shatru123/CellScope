using CellScope.Domain.Enums;

namespace CellScope.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    public UserSettings? Settings { get; set; }
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}

public class UserSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public bool LocationCollectionEnabled { get; set; } = true;
    public bool CellularCollectionEnabled { get; set; } = true;
    public bool LocalNetworkDiscoveryEnabled { get; set; } = true;
    public bool CloudSyncEnabled { get; set; } = true;
    public int DataRetentionDays { get; set; } = 90;
    public string Theme { get; set; } = "Dark";
    public int CollectionIntervalSeconds { get; set; } = 60;
    public bool BatterySavingMode { get; set; } = false;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = "Android"; // Android, Desktop, Web
    public string? Model { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? PairingCode { get; set; }
    public bool IsPaired { get; set; } = false;
    public DateTimeOffset? PairedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<CellularSnapshot> Snapshots { get; set; } = new List<CellularSnapshot>();
    public ICollection<CollectionSession> Sessions { get; set; } = new List<CollectionSession>();
}

public class CellularSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

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
    public int? SignalLevel { get; set; } // 0-4
    public double? SignalQuality { get; set; } // RSRQ (dB) or SINR (dB)

    public bool? IsRegistered { get; set; }
    public bool? IsRoaming { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? LocationAccuracy { get; set; }
    public double? Altitude { get; set; }

    public string? DataSource { get; set; } // e.g. "Android:TelephonyManager", "Demo"
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<NeighborCell> NeighborCells { get; set; } = new List<NeighborCell>();
}

public class NeighborCell
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SnapshotId { get; set; }
    public string? CellId { get; set; }
    public string? PhysicalCellId { get; set; }
    public string? TrackingAreaCode { get; set; }
    public string? RadioTechnology { get; set; }
    public string? Band { get; set; }
    public string? Frequency { get; set; }
    public int? SignalStrengthDbm { get; set; }
    public double? SignalQuality { get; set; }
    public bool? IsRegistered { get; set; }
}

public class CellObservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public string CellId { get; set; } = string.Empty;
    public string? PhysicalCellId { get; set; }
    public string? RadioTechnology { get; set; }
    public string? OperatorName { get; set; }
    public int? Mcc { get; set; }
    public int? Mnc { get; set; }
    public int? SignalStrengthDbm { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class TowerLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public int Samples { get; set; } = 1;
    public TowerConfidence Confidence { get; set; } = TowerConfidence.Medium;
    public string Source { get; set; } = "Open/public dataset";
    public string? SourceReference { get; set; }
    public DateTimeOffset LastVerified { get; set; } = DateTimeOffset.UtcNow;
}

public class LocationPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public double? Bearing { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class SignalObservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public int SignalStrengthDbm { get; set; }
    public double? SignalQuality { get; set; }
    public string? RadioTechnology { get; set; }
    public string? OperatorName { get; set; }
}

public class CellHandover
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
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

public class LocalNetwork
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? DeviceId { get; set; }
    public string Subnet { get; set; } = string.Empty;
    public string? GatewayIp { get; set; }
    public string? InterfaceName { get; set; }
    public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;
    public int TotalDevices { get; set; }

    public ICollection<NetworkDevice> Devices { get; set; } = new List<NetworkDevice>();
}

public class NetworkDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocalNetworkId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public string? Hostname { get; set; }
    public string? Vendor { get; set; }
    public NetworkDeviceType DeviceType { get; set; } = NetworkDeviceType.Unknown;
    public DateTimeOffset FirstSeen { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    public long? ResponseTimeMs { get; set; }
    public bool IsOnline { get; set; } = true;
    public string? SafeServiceSummary { get; set; }
    public string? PhoneNumber { get; set; }
}

public class CollectionSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public int IntervalSeconds { get; set; } = 60;
    public bool BatterySavingMode { get; set; } = false;
    public int SnapshotsCollected { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

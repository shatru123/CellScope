using CellScope.Application.DTOs;
using CellScope.Domain.Entities;
using CellScope.Domain.Enums;
using CellScope.Domain.Services;

namespace CellScope.Application.Mapping;

public static class DtoMapper
{
    public static CellularSnapshotDto ToDto(CellularSnapshot entity)
    {
        var rating = SignalClassifier.Classify(entity.SignalStrengthDbm, entity.RadioTechnology);
        return new CellularSnapshotDto
        {
            Id = entity.Id,
            DeviceId = entity.DeviceId,
            Timestamp = entity.Timestamp,
            OperatorName = entity.OperatorName,
            Mcc = entity.Mcc,
            Mnc = entity.Mnc,
            RadioTechnology = entity.RadioTechnology,
            CellId = entity.CellId,
            TrackingAreaCode = entity.TrackingAreaCode,
            PhysicalCellId = entity.PhysicalCellId,
            Frequency = entity.Frequency,
            Band = entity.Band,
            SignalStrengthDbm = entity.SignalStrengthDbm,
            SignalLevel = entity.SignalLevel,
            SignalQuality = entity.SignalQuality,
            SignalRating = SignalClassifier.GetRatingText(rating),
            SignalColor = SignalClassifier.GetRatingColor(rating),
            SignalPercentage = SignalClassifier.GetSignalPercentage(entity.SignalStrengthDbm),
            IsRegistered = entity.IsRegistered,
            IsRoaming = entity.IsRoaming,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            LocationAccuracy = entity.LocationAccuracy,
            Altitude = entity.Altitude,
            DataSource = entity.DataSource,
            NeighborCells = entity.NeighborCells?.Select(ToDto).ToList() ?? new List<NeighborCellDto>()
        };
    }

    public static NeighborCellDto ToDto(NeighborCell entity)
    {
        var rating = SignalClassifier.Classify(entity.SignalStrengthDbm, entity.RadioTechnology);
        return new NeighborCellDto
        {
            Id = entity.Id,
            CellId = entity.CellId,
            PhysicalCellId = entity.PhysicalCellId,
            TrackingAreaCode = entity.TrackingAreaCode,
            RadioTechnology = entity.RadioTechnology,
            Band = entity.Band,
            Frequency = entity.Frequency,
            SignalStrengthDbm = entity.SignalStrengthDbm,
            SignalQuality = entity.SignalQuality,
            SignalRating = SignalClassifier.GetRatingText(rating),
            SignalColor = SignalClassifier.GetRatingColor(rating),
            IsRegistered = entity.IsRegistered
        };
    }

    public static TowerLocationDto ToDto(TowerLocation entity, double distanceMeters = 0)
    {
        return new TowerLocationDto
        {
            Id = entity.Id,
            CellId = entity.CellId,
            PhysicalCellId = entity.PhysicalCellId,
            RadioTechnology = entity.RadioTechnology,
            Mcc = entity.Mcc,
            Mnc = entity.Mnc,
            LacTac = entity.LacTac,
            OperatorName = entity.OperatorName,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            RangeMeters = entity.RangeMeters,
            Samples = entity.Samples,
            Confidence = entity.Confidence.ToString(),
            Source = entity.Source,
            SourceReference = entity.SourceReference,
            LastVerified = entity.LastVerified,
            DistanceMeters = Math.Round(distanceMeters, 1)
        };
    }

    public static CellHandoverDto ToDto(CellHandover entity)
    {
        return new CellHandoverDto
        {
            Id = entity.Id,
            DeviceId = entity.DeviceId,
            Timestamp = entity.Timestamp,
            PreviousCellId = entity.PreviousCellId,
            NewCellId = entity.NewCellId,
            PreviousRadioTechnology = entity.PreviousRadioTechnology,
            NewRadioTechnology = entity.NewRadioTechnology,
            PreviousSignalDbm = entity.PreviousSignalDbm,
            NewSignalDbm = entity.NewSignalDbm,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            TriggerReason = entity.TriggerReason
        };
    }

    public static LocationPointDto ToDto(LocationPoint entity)
    {
        return new LocationPointDto
        {
            Id = entity.Id,
            DeviceId = entity.DeviceId,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            Accuracy = entity.Accuracy,
            Altitude = entity.Altitude,
            Speed = entity.Speed,
            Bearing = entity.Bearing,
            Timestamp = entity.Timestamp
        };
    }

    public static DeviceDto ToDto(Device entity)
    {
        return new DeviceDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Platform = entity.Platform,
            Model = entity.Model,
            OsVersion = entity.OsVersion,
            AppVersion = entity.AppVersion,
            PairingCode = entity.PairingCode,
            IsPaired = entity.IsPaired,
            PairedAt = entity.PairedAt,
            LastSeenAt = entity.LastSeenAt,
            CreatedAt = entity.CreatedAt,
            IsActive = entity.IsActive
        };
    }

    public static LocalNetworkDto ToDto(LocalNetwork entity)
    {
        return new LocalNetworkDto
        {
            Id = entity.Id,
            Subnet = entity.Subnet,
            GatewayIp = entity.GatewayIp,
            InterfaceName = entity.InterfaceName,
            ScannedAt = entity.ScannedAt,
            TotalDevices = entity.TotalDevices,
            Devices = entity.Devices?.Select(ToDto).ToList() ?? new List<NetworkDeviceDto>()
        };
    }

    public static NetworkDeviceDto ToDto(NetworkDevice entity)
    {
        string band = entity.DeviceType switch
        {
            NetworkDeviceType.Router or NetworkDeviceType.Server => "Gigabit Ethernet (1000 Mbps)",
            NetworkDeviceType.AccessPoint => "5 GHz Wi-Fi 6 Backhaul (2400 Mbps)",
            NetworkDeviceType.Laptop or NetworkDeviceType.Phone => "5 GHz Wi-Fi 6 (1200 Mbps)",
            NetworkDeviceType.TV => "5 GHz Wi-Fi (866 Mbps)",
            NetworkDeviceType.Printer or NetworkDeviceType.IoT => "2.4 GHz Wi-Fi (150 Mbps)",
            _ => "Wi-Fi / Ethernet"
        };

        int speed = entity.DeviceType switch
        {
            NetworkDeviceType.AccessPoint => 2400,
            NetworkDeviceType.Laptop or NetworkDeviceType.Phone => 1200,
            NetworkDeviceType.Router or NetworkDeviceType.Server => 1000,
            NetworkDeviceType.TV => 866,
            NetworkDeviceType.Printer or NetworkDeviceType.IoT => 150,
            _ => 100
        };

        string ipType = entity.DeviceType == NetworkDeviceType.Router ? "Static Router Gateway" : (entity.DeviceType == NetworkDeviceType.Server ? "Static Reserved IP" : "DHCP Dynamic");

        return new NetworkDeviceDto
        {
            Id = entity.Id,
            IpAddress = entity.IpAddress,
            MacAddress = entity.MacAddress,
            Hostname = entity.Hostname,
            Vendor = entity.Vendor,
            DeviceType = entity.DeviceType.ToString(),
            FirstSeen = entity.FirstSeen,
            LastSeen = entity.LastSeen,
            ResponseTimeMs = entity.ResponseTimeMs,
            IsOnline = entity.IsOnline,
            SafeServiceSummary = entity.SafeServiceSummary,
            ConnectionBand = band,
            LinkSpeedMbps = speed,
            IpAssignment = ipType
        };
    }
}

using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Domain.Enums;
using CellScope.Domain.Services;

namespace CellScope.Infrastructure.Services;

public class DemoDataService : IDemoDataService
{
    public bool IsDemoModeActive { get; set; } = true;

    private readonly Random _random = new(42);
    private int _stepIndex = 0;
    private static readonly Guid DemoDeviceId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly (double Lat, double Lon, string CellId, string Pci, string Band, string Tech, string Operator, int BaseDbm)[] _simulationRoute = new[]
    {
        (37.7749, -122.4194, "310410_12345", "102", "n78", "5G NR", "Airtel / Global Telecom", -78),
        (37.7758, -122.4180, "310410_12345", "102", "n78", "5G NR", "Airtel / Global Telecom", -81),
        (37.7767, -122.4165, "310410_12345", "102", "n78", "5G NR", "Airtel / Global Telecom", -86),
        (37.7776, -122.4150, "310410_98765", "204", "n78", "5G NR", "Airtel / Global Telecom", -75), // Handover 1
        (37.7785, -122.4140, "310410_98765", "204", "n78", "5G NR", "Airtel / Global Telecom", -72),
        (37.7798, -122.4155, "310410_98765", "204", "n78", "5G NR", "Airtel / Global Telecom", -84),
        (37.7812, -122.4185, "310410_54321", "305", "B3", "LTE", "Airtel / Global Telecom", -88),   // Handover 2
        (37.7825, -122.4210, "310410_54321", "305", "B3", "LTE", "Airtel / Global Telecom", -80),
        (37.7830, -122.4230, "310410_54321", "305", "B3", "LTE", "Airtel / Global Telecom", -74),
        (37.7820, -122.4250, "310260_67890", "412", "B28", "LTE", "Metro Wireless", -92),          // Handover 3
        (37.7790, -122.4240, "310260_67890", "412", "B28", "LTE", "Metro Wireless", -85),
        (37.7760, -122.4080, "310260_11223", "118", "n78", "5G NR", "Metro Wireless", -70)          // Handover 4
    };

    public void InitializeDemoState()
    {
        _stepIndex = 0;
    }

    public CellularSnapshotDto GenerateNextTick()
    {
        var node = _simulationRoute[_stepIndex % _simulationRoute.Length];
        _stepIndex++;

        // Add subtle realistic noise
        int jitter = _random.Next(-4, 5);
        int dbm = Math.Clamp(node.BaseDbm + jitter, -118, -55);
        double rsrq = Math.Round(-9.0 + (_random.NextDouble() * 3.0 - 1.5), 1);
        var rating = SignalClassifier.Classify(dbm, node.Tech);

        var neighbors = new List<NeighborCellDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CellId = "310410_98765",
                PhysicalCellId = "204",
                TrackingAreaCode = "54201",
                RadioTechnology = "5G NR",
                Band = "n78",
                Frequency = "3500 MHz",
                SignalStrengthDbm = dbm - 6,
                SignalQuality = -11.5,
                SignalRating = SignalClassifier.GetRatingText(SignalClassifier.Classify(dbm - 6, "5G NR")),
                SignalColor = SignalClassifier.GetRatingColor(SignalClassifier.Classify(dbm - 6, "5G NR")),
                IsRegistered = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                CellId = "310410_54321",
                PhysicalCellId = "305",
                TrackingAreaCode = "54201",
                RadioTechnology = "LTE",
                Band = "B3",
                Frequency = "1800 MHz",
                SignalStrengthDbm = dbm - 12,
                SignalQuality = -13.0,
                SignalRating = SignalClassifier.GetRatingText(SignalClassifier.Classify(dbm - 12, "LTE")),
                SignalColor = SignalClassifier.GetRatingColor(SignalClassifier.Classify(dbm - 12, "LTE")),
                IsRegistered = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                CellId = "310260_67890",
                PhysicalCellId = "412",
                TrackingAreaCode = "54202",
                RadioTechnology = "LTE",
                Band = "B28",
                Frequency = "700 MHz",
                SignalStrengthDbm = dbm - 16,
                SignalQuality = -15.2,
                SignalRating = SignalClassifier.GetRatingText(SignalClassifier.Classify(dbm - 16, "LTE")),
                SignalColor = SignalClassifier.GetRatingColor(SignalClassifier.Classify(dbm - 16, "LTE")),
                IsRegistered = false
            }
        };

        return new CellularSnapshotDto
        {
            Id = Guid.NewGuid(),
            DeviceId = DemoDeviceId,
            Timestamp = DateTimeOffset.UtcNow,
            OperatorName = node.Operator,
            Mcc = 310,
            Mnc = 410,
            RadioTechnology = node.Tech,
            CellId = node.CellId,
            TrackingAreaCode = "54201",
            PhysicalCellId = node.Pci,
            Frequency = node.Tech == "5G NR" ? "3500 MHz (n78)" : "1800 MHz (B3)",
            Band = node.Band,
            SignalStrengthDbm = dbm,
            SignalLevel = (dbm >= -80) ? 4 : (dbm >= -95 ? 3 : (dbm >= -105 ? 2 : 1)),
            SignalQuality = rsrq,
            SignalRating = SignalClassifier.GetRatingText(rating),
            SignalColor = SignalClassifier.GetRatingColor(rating),
            SignalPercentage = SignalClassifier.GetSignalPercentage(dbm),
            IsRegistered = true,
            IsRoaming = false,
            Latitude = node.Lat + (_random.NextDouble() * 0.0004 - 0.0002),
            Longitude = node.Lon + (_random.NextDouble() * 0.0004 - 0.0002),
            LocationAccuracy = 4.5,
            Altitude = 28.0,
            DataSource = "Demo Data Generator",
            NeighborCells = neighbors
        };
    }

    public IReadOnlyList<TowerLocationDto> GetDemoTowers()
    {
        var towers = new List<TowerLocationDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CellId = "310410_12345",
                PhysicalCellId = "102",
                RadioTechnology = "5G NR",
                Mcc = 310,
                Mnc = 410,
                LacTac = "54201",
                OperatorName = "Airtel / Global Telecom",
                Latitude = 37.7749,
                Longitude = -122.4194,
                RangeMeters = 1200,
                Samples = 1420,
                Confidence = "High",
                Source = "OpenCellID / MLS Dataset",
                SourceReference = "CID-310410-12345",
                LastVerified = DateTimeOffset.UtcNow.AddDays(-2),
                DistanceMeters = 45.0
            },
            new()
            {
                Id = Guid.NewGuid(),
                CellId = "310410_98765",
                PhysicalCellId = "204",
                RadioTechnology = "5G NR",
                Mcc = 310,
                Mnc = 410,
                LacTac = "54201",
                OperatorName = "Airtel / Global Telecom",
                Latitude = 37.7785,
                Longitude = -122.4140,
                RangeMeters = 1500,
                Samples = 980,
                Confidence = "High",
                Source = "OpenCellID / MLS Dataset",
                SourceReference = "CID-310410-98765",
                LastVerified = DateTimeOffset.UtcNow.AddDays(-5),
                DistanceMeters = 540.0
            },
            new()
            {
                Id = Guid.NewGuid(),
                CellId = "310410_54321",
                PhysicalCellId = "305",
                RadioTechnology = "LTE",
                Mcc = 310,
                Mnc = 410,
                LacTac = "54201",
                OperatorName = "Airtel / Global Telecom",
                Latitude = 37.7830,
                Longitude = -122.4230,
                RangeMeters = 2000,
                Samples = 3200,
                Confidence = "High",
                Source = "OpenCellID / MLS Dataset",
                SourceReference = "CID-310410-54321",
                LastVerified = DateTimeOffset.UtcNow.AddDays(-1),
                DistanceMeters = 980.0
            },
            new()
            {
                Id = Guid.NewGuid(),
                CellId = "310260_67890",
                PhysicalCellId = "412",
                RadioTechnology = "LTE",
                Mcc = 310,
                Mnc = 260,
                LacTac = "54202",
                OperatorName = "Metro Wireless",
                Latitude = 37.7710,
                Longitude = -122.4260,
                RangeMeters = 1800,
                Samples = 2100,
                Confidence = "Medium",
                Source = "OpenCellID Dataset",
                SourceReference = "CID-310260-67890",
                LastVerified = DateTimeOffset.UtcNow.AddDays(-10),
                DistanceMeters = 1120.0
            },
            new()
            {
                Id = Guid.NewGuid(),
                CellId = "310260_11223",
                PhysicalCellId = "118",
                RadioTechnology = "5G NR",
                Mcc = 310,
                Mnc = 260,
                LacTac = "54202",
                OperatorName = "Metro Wireless",
                Latitude = 37.7760,
                Longitude = -122.4080,
                RangeMeters = 900,
                Samples = 750,
                Confidence = "High",
                Source = "OpenCellID Dataset",
                SourceReference = "CID-310260-11223",
                LastVerified = DateTimeOffset.UtcNow.AddDays(-3),
                DistanceMeters = 1350.0
            }
        };

        foreach (var t in towers)
        {
            t.ConnectedDevices = GetDemoConnectedDevicesForTower(t.CellId).ToList();
        }

        return towers;
    }

    public IReadOnlyList<TowerConnectedDeviceDto> GetDemoConnectedDevicesForTower(string cellId)
    {
        var random = new Random(cellId.GetHashCode());
        var devices = new List<TowerConnectedDeviceDto>();

        var mockNodes = new[]
        {
            (Name: "Pixel 9 Pro (Live Collector)", Model: "Google Pixel 9 Pro", Type: "Mobile Collector", Platform: "Android", State: "Active Attached (Primary UE)"),
            (Name: "Galaxy S24 Ultra (Field Node)", Model: "Samsung SM-S928B", Type: "Field Telemetry Node", Platform: "Android", State: "Active Attached"),
            (Name: "CellScope Android Collector #2", Model: "OnePlus 12", Type: "Mobile Collector", Platform: "Android", State: "Active Connected"),
            (Name: "Quectel 5G Industrial Gateway", Model: "RG500Q-EA IoT Router", Type: "IoT Cellular Gateway", Platform: "Embedded Linux", State: "Continuous M2M Stream"),
            (Name: "DJI Matrice 350 RTK Drone", Model: "Cellular Dongle LTE/5G", Type: "Field Aerial Node", Platform: "Embedded OS", State: "Telemetry Uplink")
        };

        int count = random.Next(2, 5);
        for (int i = 0; i < count; i++)
        {
            var node = mockNodes[i % mockNodes.Length];
            int dbm = -70 - random.Next(5, 35);
            double rsrq = Math.Round(-8.0 - random.NextDouble() * 7.0, 1);
            int dist = random.Next(80, 1200);
            int ta = Math.Max(1, dist / 78);

            var rating = SignalClassifier.Classify(dbm, "5G NR");

            devices.Add(new TowerConnectedDeviceDto
            {
                DeviceId = Guid.NewGuid(),
                DeviceName = node.Name,
                Model = node.Model,
                DeviceType = node.Type,
                Platform = node.Platform,
                ConnectionState = node.State,
                RadioTechnology = cellId.Contains("LTE", StringComparison.OrdinalIgnoreCase) ? "LTE" : "5G NR",
                Band = cellId.Contains("LTE", StringComparison.OrdinalIgnoreCase) ? "Band 3 (1800 MHz)" : "Band n78 (3500 MHz)",
                SignalStrengthDbm = dbm,
                SignalQuality = rsrq,
                SignalRating = SignalClassifier.GetRatingText(rating),
                SignalColor = SignalClassifier.GetRatingColor(rating),
                EstimatedDistanceMeters = dist,
                TimingAdvance = ta,
                LastSeen = DateTimeOffset.UtcNow.AddSeconds(-random.Next(2, 90))
            });
        }

        return devices;
    }

    public IReadOnlyList<LocationPointDto> GetDemoTrail()
    {
        var result = new List<LocationPointDto>();
        var now = DateTimeOffset.UtcNow;
        for (int i = 0; i < _simulationRoute.Length; i++)
        {
            var node = _simulationRoute[i];
            result.Add(new LocationPointDto
            {
                Id = Guid.NewGuid(),
                DeviceId = DemoDeviceId,
                Latitude = node.Lat,
                Longitude = node.Lon,
                Accuracy = 5.0,
                Altitude = 25.0,
                Speed = 12.5,
                Bearing = 45.0,
                Timestamp = now.AddMinutes(-(_simulationRoute.Length - i) * 2)
            });
        }
        return result;
    }

    public IReadOnlyList<CellHandoverDto> GetDemoHandovers()
    {
        var now = DateTimeOffset.UtcNow;
        return new List<CellHandoverDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DeviceId = DemoDeviceId,
                Timestamp = now.AddMinutes(-6),
                PreviousCellId = "310410_12345",
                NewCellId = "310410_98765",
                PreviousRadioTechnology = "5G NR",
                NewRadioTechnology = "5G NR",
                PreviousSignalDbm = -86,
                NewSignalDbm = -75,
                Latitude = 37.7776,
                Longitude = -122.4150,
                TriggerReason = "Serving cell handover (beam optimization)"
            },
            new()
            {
                Id = Guid.NewGuid(),
                DeviceId = DemoDeviceId,
                Timestamp = now.AddMinutes(-14),
                PreviousCellId = "310410_98765",
                NewCellId = "310410_54321",
                PreviousRadioTechnology = "5G NR",
                NewRadioTechnology = "LTE",
                PreviousSignalDbm = -84,
                NewSignalDbm = -88,
                Latitude = 37.7812,
                Longitude = -122.4185,
                TriggerReason = "Inter-RAT handover (5G -> 4G Fallback)"
            },
            new()
            {
                Id = Guid.NewGuid(),
                DeviceId = DemoDeviceId,
                Timestamp = now.AddMinutes(-25),
                PreviousCellId = "310410_54321",
                NewCellId = "310260_67890",
                PreviousRadioTechnology = "LTE",
                NewRadioTechnology = "LTE",
                PreviousSignalDbm = -74,
                NewSignalDbm = -92,
                Latitude = 37.7820,
                Longitude = -122.4250,
                TriggerReason = "Inter-carrier roaming / cell reselection"
            }
        };
    }

    public bool IsDemoAdapterConnected { get; set; } = true;

    private static readonly List<NetworkDeviceDto> _demoLanDevices = new()
    {
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
            IpAddress = "192.168.1.1",
            MacAddress = "50:C7:BF:41:88:20",
            Hostname = "Archer-AX55-Router.local",
            Vendor = "TP-Link Corporation",
            DeviceType = "Router",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-30),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 1,
            IsOnline = true,
            SafeServiceSummary = "HTTP/HTTPS Web Admin, DNS, DHCP Gateway",
            ConnectionBand = "Gigabit Ethernet / Wi-Fi 6 Gateway",
            LinkSpeedMbps = 1000,
            IpAssignment = "Static Router IP"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000002"),
            IpAddress = "192.168.1.2",
            MacAddress = "AC:84:C6:92:41:10",
            Hostname = "Deco-X50-Mesh-AP.local",
            Vendor = "TP-Link Corporation",
            DeviceType = "AccessPoint",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-20),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 2,
            IsOnline = true,
            SafeServiceSummary = "Wi-Fi 6 Mesh Backhaul, IEEE 802.11ax Roaming",
            ConnectionBand = "5 GHz Wi-Fi 6 (2400 Mbps)",
            LinkSpeedMbps = 2400,
            IpAssignment = "DHCP Reserved"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000003"),
            IpAddress = "192.168.1.5",
            MacAddress = "A4:C3:F0:8A:1B:9C",
            Hostname = "MacBook-Pro-M3.local",
            Vendor = "Apple Inc.",
            DeviceType = "Laptop",
            FirstSeen = DateTimeOffset.UtcNow.AddHours(-12),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 1,
            IsOnline = true,
            SafeServiceSummary = "AirPlay 2, SSH Remote Terminal, CellScope Host",
            ConnectionBand = "5 GHz Wi-Fi 6 (1200 Mbps)",
            LinkSpeedMbps = 1200,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000004"),
            IpAddress = "192.168.1.8",
            MacAddress = "BC:D1:D3:22:90:11",
            Hostname = "Pixel-9-Pro-Collector.local",
            Vendor = "Google LLC",
            DeviceType = "Phone",
            FirstSeen = DateTimeOffset.UtcNow.AddHours(-6),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 6,
            IsOnline = true,
            SafeServiceSummary = "Android Telemetry Collector Node (SignalR Connected)",
            ConnectionBand = "5 GHz Wi-Fi 6 (1200 Mbps)",
            LinkSpeedMbps = 1200,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000005"),
            IpAddress = "192.168.1.9",
            MacAddress = "A8:42:E3:91:02:44",
            Hostname = "Galaxy-S24-Ultra.local",
            Vendor = "Samsung Electronics",
            DeviceType = "Phone",
            FirstSeen = DateTimeOffset.UtcNow.AddHours(-4),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 8,
            IsOnline = true,
            SafeServiceSummary = "SmartThings Node, Wi-Fi 6 Client Telemetry",
            ConnectionBand = "5 GHz Wi-Fi 6 (1200 Mbps)",
            LinkSpeedMbps = 1200,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000006"),
            IpAddress = "192.168.1.10",
            MacAddress = "3C:52:82:54:19:AA",
            Hostname = "iPhone-16-Pro.local",
            Vendor = "Apple Inc.",
            DeviceType = "Phone",
            FirstSeen = DateTimeOffset.UtcNow.AddHours(-2),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 5,
            IsOnline = true,
            SafeServiceSummary = "AirDrop, Apple Push Telemetry, iCloud Sync",
            ConnectionBand = "5 GHz Wi-Fi 6 (1200 Mbps)",
            LinkSpeedMbps = 1200,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000007"),
            IpAddress = "192.168.1.12",
            MacAddress = "70:88:6B:14:8A:DF",
            Hostname = "LG-webOS-OLED-TV.local",
            Vendor = "LG Electronics",
            DeviceType = "TV",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-10),
            LastSeen = DateTimeOffset.UtcNow.AddMinutes(-5),
            ResponseTimeMs = 12,
            IsOnline = true,
            SafeServiceSummary = "DIAL, DLNA 4K Media Receiver, webOS Connect",
            ConnectionBand = "5 GHz Wi-Fi (866 Mbps)",
            LinkSpeedMbps = 866,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000008"),
            IpAddress = "192.168.1.15",
            MacAddress = "F4:5C:89:12:77:33",
            Hostname = "AppleTV-4K-Bedroom.local",
            Vendor = "Apple Inc.",
            DeviceType = "TV",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-15),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 2,
            IsOnline = true,
            SafeServiceSummary = "AirPlay 2 Receiver, HomeKit Hub (Port 7000)",
            ConnectionBand = "Gigabit Ethernet (1000 Mbps)",
            LinkSpeedMbps = 1000,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000009"),
            IpAddress = "192.168.1.25",
            MacAddress = "DC:A6:32:88:12:04",
            Hostname = "HomeAssistant-Pi5.local",
            Vendor = "Raspberry Pi Foundation",
            DeviceType = "IoT",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-25),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 3,
            IsOnline = true,
            SafeServiceSummary = "MQTT Broker (Port 1883), Zigbee Home Assistant Core",
            ConnectionBand = "Gigabit Ethernet (1000 Mbps)",
            LinkSpeedMbps = 1000,
            IpAssignment = "DHCP Reserved"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000010"),
            IpAddress = "192.168.1.30",
            MacAddress = "E8:48:B8:33:44:55",
            Hostname = "Tapo-Security-Cam.local",
            Vendor = "TP-Link Corporation",
            DeviceType = "IoT",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-18),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 14,
            IsOnline = true,
            SafeServiceSummary = "RTSP Video Stream (Port 554), ONVIF 2K Security Feed",
            ConnectionBand = "2.4 GHz Wi-Fi (150 Mbps)",
            LinkSpeedMbps = 150,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000011"),
            IpAddress = "192.168.1.40",
            MacAddress = "00:11:32:98:76:54",
            Hostname = "Synology-DS923-NAS.local",
            Vendor = "Synology Inc.",
            DeviceType = "Server",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-40),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 2,
            IsOnline = true,
            SafeServiceSummary = "Synology DSM (5000), SMB/NFS File Share (445), Docker Host",
            ConnectionBand = "Dual 1Gbps LACP Ethernet (2000 Mbps)",
            LinkSpeedMbps = 2000,
            IpAssignment = "Static IP"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000012"),
            IpAddress = "192.168.1.55",
            MacAddress = "00:1E:58:AA:BB:CC",
            Hostname = "LaserJet-Pro-Office.local",
            Vendor = "D-Link / HP Inc.",
            DeviceType = "Printer",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-22),
            LastSeen = DateTimeOffset.UtcNow.AddHours(-1),
            ResponseTimeMs = 9,
            IsOnline = true,
            SafeServiceSummary = "IPP / RAW Port 9100 Print Server, AirPrint, SNMP",
            ConnectionBand = "2.4 GHz Wi-Fi (300 Mbps)",
            LinkSpeedMbps = 300,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000013"),
            IpAddress = "192.168.1.60",
            MacAddress = "58:CB:52:6A:11:80",
            Hostname = "PlayStation-5-Console.local",
            Vendor = "Sony Interactive Entertainment",
            DeviceType = "IoT",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-12),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 4,
            IsOnline = true,
            SafeServiceSummary = "PlayStation Network, Remote Play, Media Server",
            ConnectionBand = "Gigabit Ethernet (1000 Mbps)",
            LinkSpeedMbps = 1000,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000014"),
            IpAddress = "192.168.1.72",
            MacAddress = "FC:65:DE:11:22:33",
            Hostname = "Echo-Studio-Audio.local",
            Vendor = "Amazon Technologies",
            DeviceType = "IoT",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-15),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 11,
            IsOnline = true,
            SafeServiceSummary = "Alexa Voice Assistant, Spotify Connect, mDNS",
            ConnectionBand = "5 GHz Wi-Fi (433 Mbps)",
            LinkSpeedMbps = 433,
            IpAssignment = "DHCP Dynamic"
        },
        new()
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000015"),
            IpAddress = "192.168.1.88",
            MacAddress = "48:D7:05:77:88:99",
            Hostname = "Nest-Learning-Thermostat.local",
            Vendor = "Google LLC",
            DeviceType = "IoT",
            FirstSeen = DateTimeOffset.UtcNow.AddDays(-35),
            LastSeen = DateTimeOffset.UtcNow,
            ResponseTimeMs = 18,
            IsOnline = true,
            SafeServiceSummary = "Google Nest Weave, Smart HVAC Climate Control",
            ConnectionBand = "2.4 GHz Wi-Fi (72 Mbps)",
            LinkSpeedMbps = 72,
            IpAssignment = "DHCP Dynamic"
        }
    };

    public LocalNetworkDto GetDemoLocalNetwork()
    {
        return new LocalNetworkDto
        {
            Id = Guid.NewGuid(),
            Subnet = "192.168.1.0/24",
            GatewayIp = "192.168.1.1",
            InterfaceName = "en0 (Wi-Fi 6 / 802.11ax)",
            ScannedAt = DateTimeOffset.UtcNow,
            TotalDevices = _demoLanDevices.Count,
            IsAdapterConnected = IsDemoAdapterConnected,
            Devices = _demoLanDevices.Select(d => new NetworkDeviceDto
            {
                Id = d.Id,
                IpAddress = d.IpAddress,
                MacAddress = d.MacAddress,
                Hostname = d.Hostname,
                Vendor = d.Vendor,
                DeviceType = d.DeviceType,
                FirstSeen = d.FirstSeen,
                LastSeen = d.LastSeen,
                ResponseTimeMs = d.ResponseTimeMs,
                IsOnline = IsDemoAdapterConnected ? d.IsOnline : false,
                SafeServiceSummary = d.SafeServiceSummary,
                ConnectionBand = d.ConnectionBand,
                LinkSpeedMbps = d.LinkSpeedMbps,
                IpAssignment = d.IpAssignment
            }).ToList()
        };
    }

    public NetworkDeviceDto? ToggleDemoDeviceConnection(Guid id)
    {
        var dev = _demoLanDevices.FirstOrDefault(d => d.Id == id);
        if (dev != null)
        {
            dev.IsOnline = !dev.IsOnline;
            dev.LastSeen = DateTimeOffset.UtcNow;
            return dev;
        }
        return null;
    }

    public NetworkDeviceDto? SetDemoDeviceConnection(Guid id, bool isConnected)
    {
        var dev = _demoLanDevices.FirstOrDefault(d => d.Id == id);
        if (dev != null)
        {
            dev.IsOnline = isConnected;
            dev.LastSeen = DateTimeOffset.UtcNow;
            return dev;
        }
        return null;
    }

    public LocalNetworkDto SetAllDemoDevicesConnection(bool isConnected)
    {
        foreach (var dev in _demoLanDevices)
        {
            dev.IsOnline = isConnected;
            dev.LastSeen = DateTimeOffset.UtcNow;
        }
        return GetDemoLocalNetwork();
    }

    public bool ToggleDemoAdapter()
    {
        IsDemoAdapterConnected = !IsDemoAdapterConnected;
        return IsDemoAdapterConnected;
    }

    public SignalAnalyticsDto GetDemoAnalytics(string timeRange)
    {
        var now = DateTimeOffset.UtcNow;
        var dto = new SignalAnalyticsDto
        {
            TotalObservations = 180,
            TotalHandovers = 4,
            AverageSignalStrength = -81.4,
            MinSignalStrength = -102,
            MaxSignalStrength = -68
        };

        for (int i = 60; i >= 0; i--)
        {
            var time = now.AddMinutes(-i * 4);
            int baseVal = -80 + (int)(Math.Sin(i / 5.0) * 12.0) + _random.Next(-3, 4);
            dto.SignalStrengthTrend.Add(new TimeSeriesPoint<int>
            {
                Timestamp = time,
                Value = Math.Clamp(baseVal, -110, -60),
                Label = time.ToString("HH:mm")
            });

            dto.SignalQualityTrend.Add(new TimeSeriesPoint<double>
            {
                Timestamp = time,
                Value = Math.Round(-9.5 + Math.Sin(i / 6.0) * 3.0, 1),
                Label = time.ToString("HH:mm")
            });
        }

        dto.TechnologyDistribution = new Dictionary<string, int>
        {
            { "5G NR", 112 },
            { "LTE", 64 },
            { "WCDMA / 3G", 4 }
        };

        dto.OperatorAverageSignal = new Dictionary<string, double>
        {
            { "Airtel / Global Telecom", -79.2 },
            { "Metro Wireless", -84.8 }
        };

        dto.RatingDistribution = new Dictionary<string, int>
        {
            { "Excellent", 48 },
            { "Good", 86 },
            { "Fair", 38 },
            { "Poor", 8 }
        };

        return dto;
    }
}

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
        return new List<TowerLocationDto>
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

    public LocalNetworkDto GetDemoLocalNetwork()
    {
        return new LocalNetworkDto
        {
            Id = Guid.NewGuid(),
            Subnet = "192.168.1.0/24",
            GatewayIp = "192.168.1.1",
            InterfaceName = "en0 (Wi-Fi 6 / 802.11ax)",
            ScannedAt = DateTimeOffset.UtcNow,
            TotalDevices = 5,
            Devices = new List<NetworkDeviceDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IpAddress = "192.168.1.1",
                    MacAddress = "50:C7:BF:41:88:20",
                    Hostname = "Archer-AX55.local",
                    Vendor = "TP-Link Corporation",
                    DeviceType = "Router",
                    FirstSeen = DateTimeOffset.UtcNow.AddDays(-14),
                    LastSeen = DateTimeOffset.UtcNow,
                    ResponseTimeMs = 2,
                    IsOnline = true,
                    SafeServiceSummary = "HTTP/HTTPS Web Admin, DNS, DHCP Gateway"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    IpAddress = "192.168.1.5",
                    MacAddress = "A4:C3:F0:8A:1B:9C",
                    Hostname = "MacBook-Pro.local",
                    Vendor = "Apple Inc.",
                    DeviceType = "Laptop",
                    FirstSeen = DateTimeOffset.UtcNow.AddHours(-6),
                    LastSeen = DateTimeOffset.UtcNow,
                    ResponseTimeMs = 1,
                    IsOnline = true,
                    SafeServiceSummary = "AirPlay 2, SSH, CellScope Host"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    IpAddress = "192.168.1.8",
                    MacAddress = "BC:D1:D3:22:90:11",
                    Hostname = "Pixel-9-Pro.local",
                    Vendor = "Google LLC",
                    DeviceType = "Phone",
                    FirstSeen = DateTimeOffset.UtcNow.AddHours(-3),
                    LastSeen = DateTimeOffset.UtcNow,
                    ResponseTimeMs = 7,
                    IsOnline = true,
                    SafeServiceSummary = "Android Telemetry Collector Node"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    IpAddress = "192.168.1.12",
                    MacAddress = "70:88:6B:14:8A:DF",
                    Hostname = "LG-webOS-OLED.local",
                    Vendor = "LG Electronics",
                    DeviceType = "TV",
                    FirstSeen = DateTimeOffset.UtcNow.AddDays(-5),
                    LastSeen = DateTimeOffset.UtcNow.AddMinutes(-8),
                    ResponseTimeMs = 12,
                    IsOnline = true,
                    SafeServiceSummary = "DIAL, DLNA Media Receiver"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    IpAddress = "192.168.1.25",
                    MacAddress = "DC:A6:32:88:12:04",
                    Hostname = "HomeSensor-Pi4.local",
                    Vendor = "Raspberry Pi Foundation",
                    DeviceType = "IoT",
                    FirstSeen = DateTimeOffset.UtcNow.AddDays(-10),
                    LastSeen = DateTimeOffset.UtcNow,
                    ResponseTimeMs = 4,
                    IsOnline = true,
                    SafeServiceSummary = "MQTT Broker (Port 1883), Prometheus Exporter"
                }
            }
        };
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

using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Domain.Enums;
using CellScope.Domain.Services;

namespace CellScope.Infrastructure.Services;

public class DemoDataService : IDemoDataService
{
    private bool _isDemoModeActive = false; // Default to Strict Real-Only Mode
    public bool IsDemoModeActive
    {
        get => _isDemoModeActive;
        set
        {
            if (_isDemoModeActive != value)
            {
                _isDemoModeActive = value;
                if (_isDemoModeActive)
                {
                    InitializeDemoState();
                }
                OnModeChanged?.Invoke();
            }
        }
    }

    public event Action? OnModeChanged;

    public void SetMode(bool isDemoMode)
    {
        IsDemoModeActive = isDemoMode;
    }

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
            var seedRandom = new Random(t.CellId.GetHashCode());
            t.TotalConnectedDevices = seedRandom.Next(1850, 4200);
            t.ActiveDataSessions = (int)(t.TotalConnectedDevices * 0.84);
            t.VoLteVoiceChannels = (int)(t.TotalConnectedDevices * 0.12);
            t.IoTTelemetryNodes = t.TotalConnectedDevices - t.ActiveDataSessions - t.VoLteVoiceChannels;
            t.AggregateThroughputMbps = Math.Round(420.0 + seedRandom.NextDouble() * 460.0, 1);
            t.PrbUtilizationPercent = Math.Round(68.0 + seedRandom.NextDouble() * 24.0, 1);
            t.ConnectedDevices = GetDemoConnectedDevicesForTower(t.CellId).ToList();
            t.ActiveCalls = GetDemoActiveCallsForTower(t.CellId).ToList();
        }

        return towers;
    }

    public IReadOnlyList<TowerConnectedDeviceDto> GetDemoConnectedDevicesForTower(string cellId)
    {
        var random = new Random(cellId.GetHashCode());
        var devices = new List<TowerConnectedDeviceDto>();

        var fullSubscribers = new (string Name, string Model, string Type, string Plat, string Band, string Phone, string Modulation, double Throughput)[]
        {
            ("Samsung Galaxy S25 Ultra", "SM-S938B 5G", "Smartphone", "Android 15", "Band n78 (3500 MHz)", "+91 96044 66334", "256-QAM", 285.4),
            ("Apple iPhone 16 Pro Max", "A3296 5G NR", "Smartphone", "iOS 18", "Band n78 (3500 MHz)", "+91 98231 54128", "256-QAM", 312.0),
            ("Google Pixel 9 Pro (Field Node)", "GC3VE 5G", "Smartphone", "Android 15", "Band n78 (3500 MHz)", "+91 98901 44210", "256-QAM", 240.5),
            ("Realme GT 6 5G", "RMX3850", "Smartphone", "Android 14", "Band 3 (1800 MHz)", "+91 98224 51920", "64-QAM", 115.0),
            ("OnePlus 12 5G", "CPH2581", "Smartphone", "Android 14", "Band n78 (3500 MHz)", "+91 97654 32109", "256-QAM", 195.2),
            ("Xiaomi 14 Ultra", "24030PN60G", "Smartphone", "HyperOS / Android", "Band n78 (3500 MHz)", "+91 94221 88390", "256-QAM", 270.8),
            ("Samsung Galaxy S24+", "SM-S926B 5G", "Smartphone", "Android 14", "Band 3 (1800 MHz)", "+91 98500 12345", "64-QAM", 130.0),
            ("Lenovo ThinkPad X1 5G WWAN", "Quectel EM120R 5G", "Laptop", "Windows 11 Pro", "Band n78 (3500 MHz)", "+91 70281 99012 (eSIM)", "256-QAM", 340.0),
            ("Dell Latitude 9440 5G", "Snapdragon X75 5G", "Laptop", "Windows 11 Pro", "Band n78 (3500 MHz)", "+91 70281 99013 (eSIM)", "256-QAM", 295.6),
            ("Apple MacBook Pro 5G Tether", "iPhone Hotspot Gateway", "Laptop", "macOS Sonoma", "Band n78 (3500 MHz)", "+91 96044 66334 (Tether)", "256-QAM", 220.4),
            ("HP EliteBook 840 G10 5G", "Intel 5G Solution 5000", "Laptop", "Windows 11", "Band n78 (3500 MHz)", "+91 70281 99014 (eSIM)", "256-QAM", 260.0),
            ("Microsoft Surface Pro 10 5G", "Snapdragon X Elite", "Laptop", "Windows 11 ARM", "Band n78 (3500 MHz)", "+91 70281 99015 (eSIM)", "256-QAM", 310.0),
            ("DJI Matrice 350 RTK Field Drone", "DJI Cellular Dongle 2", "Drone", "Embedded Linux", "Band 3 (1800 MHz)", "+91 80071 44556 (eSIM)", "64-QAM", 48.5),
            ("DJI Inspire 3 Aerial Node", "DJI Pro Cellular 5G", "Drone", "Embedded RTOS", "Band n78 (3500 MHz)", "+91 80071 44557 (eSIM)", "256-QAM", 85.0),
            ("Skydio X2 Autonomous Drone", "Skydio 5G Link", "Drone", "Embedded Linux", "Band 3 (1800 MHz)", "+91 80071 44558 (eSIM)", "64-QAM", 38.0),
            ("Quectel RG500Q-EA 5G Gateway", "Industrial M2M Gateway", "IoT", "Embedded Linux", "Band n78 (3500 MHz)", "+91 80071 44559 (M2M)", "256-QAM", 185.0),
            ("Cisco Catalyst Cellular Gateway", "CG522-E 5G Gigabit", "IoT", "Cisco IOS-XE", "Band n78 (3500 MHz)", "+91 80071 44560 (M2M)", "256-QAM", 450.0),
            ("Telit Cinterion FN990A 5G", "Smart Grid Node #402", "IoT", "Embedded RTOS", "Band 28 (700 MHz)", "+91 80071 44561 (M2M)", "16-QAM", 12.4),
            ("Siemens Scalance 5G Router", "MUM856-1 LTE/5G", "IoT", "Siemens SINEMA", "Band n78 (3500 MHz)", "+91 80071 44562 (M2M)", "256-QAM", 210.0),
            ("Schneider Electric Grid Sensor", "EcoStruxure 5G IoT", "IoT", "RTOS", "Band 28 (700 MHz)", "+91 80071 44563 (M2M)", "16-QAM", 8.5),
            ("Apple iPhone 15", "A3090 5G", "Smartphone", "iOS 17.5", "Band 3 (1800 MHz)", "+91 98235 66789", "64-QAM", 95.0),
            ("Samsung Galaxy A55 5G", "SM-A556B", "Smartphone", "Android 14", "Band 28 (700 MHz)", "+91 98908 77654", "64-QAM", 72.0),
            ("Vivo X100 Pro", "V2309A 5G", "Smartphone", "OriginOS 4", "Band n78 (3500 MHz)", "+91 97645 11223", "256-QAM", 210.0),
            ("Motorola Edge 50 Ultra", "XT2401-1", "Smartphone", "Android 14", "Band n78 (3500 MHz)", "+91 98220 99887", "256-QAM", 180.5),
            ("Nothing Phone (2a)", "A142 5G", "Smartphone", "Nothing OS 2.5", "Band n78 (3500 MHz)", "+91 98221 44332", "256-QAM", 165.0),
            ("Poco F6 Pro 5G", "23113RKC6G", "Smartphone", "HyperOS", "Band n78 (3500 MHz)", "+91 98902 33441", "256-QAM", 230.0),
            ("Honor Magic6 Pro", "BVL-AN16", "Smartphone", "MagicOS 8.0", "Band n78 (3500 MHz)", "+91 97651 88990", "256-QAM", 275.0),
            ("Oppo Find X7 Ultra", "PHY110", "Smartphone", "ColorOS 14", "Band n78 (3500 MHz)", "+91 94220 55667", "256-QAM", 260.0),
            ("Asus ROG Phone 8 Pro", "ASUS_AI2401_A", "Smartphone", "ROG UI / Android 14", "Band n78 (3500 MHz)", "+91 98230 77889", "256-QAM", 320.0),
            ("Sony Xperia 1 VI", "XQ-EC54", "Smartphone", "Android 14", "Band n78 (3500 MHz)", "+91 98907 66554", "256-QAM", 215.0),
            ("Samsung Galaxy Z Fold 6", "SM-F956B", "Smartphone", "One UI 6.1.1", "Band n78 (3500 MHz)", "+91 97644 33221", "256-QAM", 290.0),
            ("Samsung Galaxy Z Flip 6", "SM-F741B", "Smartphone", "One UI 6.1.1", "Band 3 (1800 MHz)", "+91 94229 11223", "64-QAM", 140.0),
            ("Google Pixel 8a", "G8HHN 5G", "Smartphone", "Android 14", "Band 3 (1800 MHz)", "+91 98239 88776", "64-QAM", 125.0),
            ("Realme 12 Pro+ 5G", "RMX3840", "Smartphone", "Realme UI 5.0", "Band 3 (1800 MHz)", "+91 98909 22334", "64-QAM", 110.0),
            ("Vivo V30 Pro", "V2319", "Smartphone", "Funtouch OS 14", "Band n78 (3500 MHz)", "+91 97650 99887", "256-QAM", 175.0),
            ("Xiaomi Redmi Note 13 Pro+", "23090RA98G", "Smartphone", "HyperOS", "Band 3 (1800 MHz)", "+91 94228 77665", "64-QAM", 98.0),
            ("OnePlus Nord 4 5G", "CPH2661", "Smartphone", "OxygenOS 14.1", "Band n78 (3500 MHz)", "+91 98227 66554", "256-QAM", 190.0),
            ("Motorola Razr 50 Ultra", "XT2451-2", "Smartphone", "Android 14", "Band n78 (3500 MHz)", "+91 98906 55443", "256-QAM", 205.0),
            ("Apple iPhone 16 Plus", "A3290 5G", "Smartphone", "iOS 18", "Band n78 (3500 MHz)", "+91 97643 44332", "256-QAM", 280.0),
            ("Samsung Galaxy S23 FE", "SM-S711B", "Smartphone", "One UI 6.1", "Band 3 (1800 MHz)", "+91 94227 33221", "64-QAM", 120.0),
            ("JioBharat 4G Companion Node", "Jio-4G-V2", "IoT", "ThreadX", "Band 28 (700 MHz)", "+91 94200 33445", "QPSK", 8.2),
            ("Sierra Wireless AirLink XR90", "5G Mobile Router", "IoT", "AirLink OS", "Band n78 (3500 MHz)", "+91 80071 44564 (M2M)", "256-QAM", 380.0)
        };

        for (int i = 0; i < fullSubscribers.Length; i++)
        {
            var s = fullSubscribers[i];
            int dbm = -65 - random.Next(2, 42);
            int dist = random.Next(85, 1850);
            var rating = SignalClassifier.Classify(dbm, cellId.Contains("LTE", StringComparison.OrdinalIgnoreCase) ? "LTE" : "5G NR");
            long maskedSuffix = 1000 + (Math.Abs(cellId.GetHashCode()) + i * 137) % 8999;

            devices.Add(new TowerConnectedDeviceDto
            {
                DeviceId = Guid.NewGuid(),
                DeviceName = s.Name,
                Model = s.Model,
                DeviceType = s.Type,
                Platform = s.Plat,
                RadioTechnology = cellId.Contains("LTE", StringComparison.OrdinalIgnoreCase) ? "LTE" : "5G NR",
                Band = s.Band,
                PhoneNumber = s.Phone,
                MaskedImei = $"86{random.Next(10, 99)}4005****{maskedSuffix}",
                Modulation = s.Modulation,
                ThroughputMbps = Math.Round(s.Throughput * (0.8 + random.NextDouble() * 0.4), 1),
                SignalStrengthDbm = dbm,
                SignalQuality = Math.Round(-7.0 - random.NextDouble() * 8.0, 1),
                SignalRating = SignalClassifier.GetRatingText(rating),
                SignalColor = SignalClassifier.GetRatingColor(rating),
                EstimatedDistanceMeters = dist,
                TimingAdvance = Math.Max(1, dist / 78),
                LastSeen = DateTimeOffset.UtcNow.AddMinutes(-random.Next(1, 60)),
                ConnectionState = "RRC_CONNECTED (Active Carrier Aggregation)"
            });
        }

        return devices;
    }

    public IReadOnlyList<ActiveCallSessionDto> GetDemoActiveCallsForTower(string cellId)
    {
        var random = new Random(cellId.GetHashCode() + 999);
        var calls = new List<ActiveCallSessionDto>();

        var sampleCalls = new (string FromNum, string FromName, string ToNum, string ToName, string Type, string Status, int DurationSec, string Codec, double Mos, string Bearer)[]
        {
            ("+91 98220 11223", "Sector Subscriber Node #101", "+91 98231 54128", "Enterprise Cloud Gateway (Pune)", "VoNR 5G Ultra-HD", "Active (In-Call)", 258, "EVS-SWB 24.4 kbps", 4.5, "5QI-1 (Conversational Voice)"),
            ("+91 98224 51920", "Realme 5s Smartphone", "+91 97654 32109", "OnePlus Node (Mumbai HQ)", "VoLTE HD Voice", "Active (In-Call)", 712, "AMR-WB 23.85 kbps", 4.2, "QCI-1 (IMS Voice)"),
            ("+91 94221 88390", "Connected Mobile Client", "+91 98901 44210", "Field Operations Node (Delhi)", "VoNR 5G Ultra-HD", "Ringing (Alerting)", 8, "EVS-SWB 24.4 kbps", 4.4, "5QI-1 (Conversational Voice)"),
            ("+91 98500 12345", "Samsung Galaxy S24+", "+91 98235 66789", "iPhone 15 Subscriber (Bangalore)", "VoLTE HD Voice", "Active (In-Call)", 110, "AMR-WB 12.65 kbps", 4.1, "QCI-1 (IMS Voice)"),
            ("+91 70281 99012 (eSIM)", "Lenovo ThinkPad X1 5G WWAN", "+91 80071 44560 (M2M)", "Cisco SIP Unified Trunk", "5G Video HD Conference", "Active (In-Call)", 1930, "Opus HD 64 kbps", 4.6, "5QI-2 (Conversational Video)"),
            ("+91 80071 44556 (eSIM)", "DJI Matrice 350 RTK Drone", "+91 70281 99013 (eSIM)", "Ground Control Station (GCS)", "Mission Critical PTT (MCPTT)", "Active Voice Stream", 862, "AMR-WB 23.85 kbps", 4.3, "5QI-65 (Mission Critical Voice)"),
            ("+91 98231 54128", "Apple iPhone 16 Pro Max", "+91 97645 11223", "Vivo X100 Pro (Hyderabad)", "VoNR 5G Ultra-HD", "Active (In-Call)", 445, "EVS-FB 128 kbps", 4.7, "5QI-1 (Conversational Voice)"),
            ("+91 98901 44210", "Google Pixel 9 Pro", "+91 98220 99887", "Motorola Edge 50 Ultra", "VoNR 5G Ultra-HD", "Active (In-Call)", 320, "EVS-SWB 24.4 kbps", 4.4, "5QI-1 (Conversational Voice)"),
            ("+91 97654 32109", "OnePlus 12 5G", "+91 94220 55667", "Oppo Find X7 Ultra", "VoWiFi (Wi-Fi Calling)", "Active (In-Call)", 540, "AMR-WB 23.85 kbps", 4.3, "QCI-1 (IMS Voice)"),
            ("+91 94221 88390", "Xiaomi 14 Ultra", "+91 98221 44332", "Nothing Phone (2a)", "VoNR 5G Ultra-HD", "Active (In-Call)", 184, "EVS-SWB 24.4 kbps", 4.5, "5QI-1 (Conversational Voice)"),
            ("+91 98908 77654", "Samsung Galaxy A55 5G", "+91 98902 33441", "Poco F6 Pro 5G", "VoLTE HD Voice", "Active (In-Call)", 62, "AMR-WB 12.65 kbps", 4.0, "QCI-1 (IMS Voice)"),
            ("+91 97651 88990", "Honor Magic6 Pro", "+91 98230 77889", "Asus ROG Phone 8 Pro", "VoNR 5G Ultra-HD", "Active (In-Call)", 940, "EVS-SWB 24.4 kbps", 4.6, "5QI-1 (Conversational Voice)"),
            ("+91 98907 66554", "Sony Xperia 1 VI", "+91 97644 33221", "Samsung Galaxy Z Fold 6", "VoNR 5G Ultra-HD", "Active (In-Call)", 150, "EVS-SWB 24.4 kbps", 4.5, "5QI-1 (Conversational Voice)"),
            ("+91 94229 11223", "Samsung Galaxy Z Flip 6", "+91 98239 88776", "Google Pixel 8a", "VoLTE HD Voice", "Active (In-Call)", 480, "AMR-WB 23.85 kbps", 4.2, "QCI-1 (IMS Voice)"),
            ("+91 98909 22334", "Realme 12 Pro+ 5G", "+91 97650 99887", "Vivo V30 Pro", "VoLTE HD Voice", "Establishing Call", 4, "AMR-WB 12.65 kbps", 4.1, "QCI-1 (IMS Voice)"),
            ("+91 94228 77665", "Xiaomi Redmi Note 13 Pro+", "+91 98227 66554", "OnePlus Nord 4 5G", "VoLTE HD Voice", "Active (In-Call)", 890, "AMR-WB 23.85 kbps", 4.3, "QCI-1 (IMS Voice)"),
            ("+91 98906 55443", "Motorola Razr 50 Ultra", "+91 97643 44332", "Apple iPhone 16 Plus", "VoNR 5G Ultra-HD", "Active (In-Call)", 610, "EVS-FB 128 kbps", 4.7, "5QI-1 (Conversational Voice)"),
            ("+91 94227 33221", "Samsung Galaxy S23 FE", "+91 94225 66778", "Regional Gateway Node", "VoNR 5G Ultra-HD", "Active (In-Call)", 135, "EVS-SWB 24.4 kbps", 4.5, "5QI-1 (Conversational Voice)"),
            ("+91 70281 99014 (eSIM)", "HP EliteBook 840 5G", "+91 80071 44559 (M2M)", "Quectel Telemetry Server", "Data Bearer VoLTE", "Active (In-Call)", 1250, "Opus HD 64 kbps", 4.4, "5QI-2 (Conversational Video)"),
            ("+91 80071 44557 (eSIM)", "DJI Inspire 3 Aerial Node", "+91 80071 44558 (eSIM)", "Skydio Fleet Dispatch", "Mission Critical PTT (MCPTT)", "Active Voice Stream", 410, "AMR-WB 23.85 kbps", 4.3, "5QI-65 (Mission Critical Voice)")
        };

        foreach (var c in sampleCalls)
        {
            int jitterSec = random.Next(-15, 25);
            int finalSec = Math.Max(1, c.DurationSec + jitterSec);
            string durationFormatted = $"{finalSec / 60:D2}:{finalSec % 60:D2}";

            calls.Add(new ActiveCallSessionDto
            {
                SessionId = Guid.NewGuid(),
                CallerNumber = c.FromNum,
                CallerName = c.FromName,
                ReceiverNumber = c.ToNum,
                ReceiverName = c.ToName,
                CallType = c.Type,
                Status = c.Status,
                DurationSeconds = finalSec,
                Duration = durationFormatted,
                Codec = c.Codec,
                MosScore = Math.Round(Math.Clamp(c.Mos + (random.NextDouble() * 0.2 - 0.1), 3.8, 4.9), 1),
                RadioBearer = c.Bearer,
                CellId = cellId,
                SignalRating = c.Mos >= 4.4 ? "Excellent" : "Good",
                SignalColor = c.Mos >= 4.4 ? "#10B981" : "#06B6D4",
                StartedAt = DateTimeOffset.UtcNow.AddSeconds(-finalSec)
            });
        }

        return calls;
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
            PhoneNumber = "+91 98231 54128",
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
            PhoneNumber = "+91 96044 66334",
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
            PhoneNumber = "+91 98224 51920",
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
                PhoneNumber = d.PhoneNumber,
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

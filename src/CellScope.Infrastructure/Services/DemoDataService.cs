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

    public IReadOnlyList<TowerLocationDto> GetDemoTowers() => GetDemoTowers(null, null, 8000);

    public IReadOnlyList<TowerLocationDto> GetDemoTowers(double? latitude = null, double? longitude = null, double radiusMeters = 8000)
    {
        double centerLat = latitude ?? 37.7749;
        double centerLon = longitude ?? -122.4194;

        var towers = GenerateDynamicDemoTowers(centerLat, centerLon, radiusMeters);

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

    private static List<TowerLocationDto> GenerateDynamicDemoTowers(double centerLat, double centerLon, double radiusMeters)
    {
        var towers = new List<TowerLocationDto>();
        var random = new Random(HashCode.Combine((int)(Math.Round(centerLat, 2) * 100), (int)(Math.Round(centerLon, 2) * 100)));

        var sectorTemplates = new (double DistFraction, double Angle, string Tech, string Band, string Pci, string Op, string Suffix, string Conf, int Samples)[]
        {
            (0.04, 35.0, "5G NR", "Band n78 (3500 MHz)", "102", "Airtel / Primary 5G gNodeB", "101", "High", 2800),
            (0.10, 125.0, "5G NR", "Band n78 (3500 MHz)", "204", "Telecom Ultra-HD 5G (n78)", "102", "High", 2100),
            (0.18, 215.0, "LTE", "Band 3 (1800 MHz)", "305", "Urban Macro LTE Station", "201", "High", 3200),
            (0.25, 305.0, "LTE", "Band 28 (700 MHz)", "412", "Suburban Coverage LTE Sector", "202", "Medium", 1450),
            (0.32, 75.0, "5G NR", "Band n28 (700 MHz)", "118", "Sub-6 Long Range 5G Cell", "301", "High", 1950),
            (0.40, 165.0, "LTE", "Band 1 (2100 MHz)", "520", "High Capacity Metro LTE", "302", "High", 2650),
            (0.48, 255.0, "5G NR", "Band n77 (3700 MHz)", "224", "C-Band Gigabit Micro gNodeB", "401", "High", 1820),
            (0.55, 345.0, "5G NR", "Band n258 (28 GHz)", "330", "mmWave High Density Node", "402", "Medium", 920),
            (0.62, 45.0, "LTE", "Band 7 (2600 MHz)", "615", "High Band LTE Sector", "501", "High", 2400),
            (0.70, 135.0, "5G NR", "Band n78 (3500 MHz)", "418", "Enterprise Campus 5G Cell", "502", "High", 3100),
            (0.78, 225.0, "LTE", "Band 20 (800 MHz)", "725", "Rural Highway LTE Mast", "601", "Medium", 1100),
            (0.85, 315.0, "5G NR", "Band n78 (3500 MHz)", "512", "Regional Macro gNodeB", "602", "High", 2780),
            (0.15, 90.0, "5G NR", "Band n78 (3500 MHz)", "115", "Carrier Aggregation 5G Node", "701", "High", 2340),
            (0.28, 180.0, "LTE", "Band 40 (2300 MHz)", "630", "TDD Capacity LTE Sector", "702", "High", 2890),
            (0.44, 270.0, "5G NR", "Band n78 (3500 MHz)", "240", "Public Safety & Emergency 5G", "801", "High", 1650),
            (0.58, 0.0, "LTE", "Band 8 (900 MHz)", "810", "Extended Coverage Base Station", "802", "Medium", 1320),
            (0.74, 150.0, "5G NR", "Band n77 (3700 MHz)", "345", "Mid-Band Commercial 5G", "901", "High", 2980),
            (0.90, 290.0, "LTE", "Band 3 (1800 MHz)", "920", "Perimeter Macro LTE Mast", "902", "Medium", 1540)
        };

        for (int i = 0; i < sectorTemplates.Length; i++)
        {
            var s = sectorTemplates[i];
            double dist = Math.Max(180.0, s.DistFraction * radiusMeters);
            var (tLat, tLon) = GeodesyUtils.CalculateOffsetCoordinates(centerLat, centerLon, dist, s.Angle);
            var (area, street, city, zip) = ResolveGeographicAddress(tLat, tLon, i, s.Tech);

            towers.Add(new TowerLocationDto
            {
                Id = Guid.NewGuid(),
                CellId = $"310410_{s.Suffix}_{random.Next(1000, 9999)}",
                PhysicalCellId = s.Pci,
                RadioTechnology = s.Tech,
                Mcc = 310,
                Mnc = 410,
                LacTac = "54201",
                OperatorName = s.Op,
                Latitude = Math.Round(tLat, 6),
                Longitude = Math.Round(tLon, 6),
                Area = area,
                StreetAddress = street,
                City = city,
                PostalCode = zip,
                RangeMeters = (int)(dist * 1.3),
                Samples = s.Samples,
                Confidence = s.Conf,
                Source = "OpenCellID / MLS Global Cellular Dataset",
                SourceReference = $"OCID-GLOBAL-{random.Next(100000, 999999)}",
                LastVerified = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 15)),
                DistanceMeters = dist
            });
        }

        return towers.OrderBy(t => t.DistanceMeters).ToList();
    }

    private readonly record struct GeoAnchor(
        double Lat,
        double Lon,
        string Area,
        string Street,
        string City,
        string PostalCode);

    private readonly record struct MetroRegion(
        string RegionName,
        double MinLat,
        double MaxLat,
        double MinLon,
        double MaxLon,
        GeoAnchor[] Anchors);

    private static readonly MetroRegion[] DefinedMetroRegions = new[]
    {
        // San Francisco Bay Area & Silicon Valley (Lat 37.20 .. 38.10, Lon -122.65 .. -121.75)
        new MetroRegion(
            "San Francisco Bay Area", 37.20, 38.10, -122.65, -121.75,
            new GeoAnchor[]
            {
                new(37.7946, -122.3999, "Financial District", "742 Market Street, Suite 400", "San Francisco", "CA 94103"),
                new(37.7785, -122.4056, "SoMa Tech Corridor", "500 Howard Street / 1st St", "San Francisco", "CA 94105"),
                new(37.7599, -122.4148, "Mission District", "2196 Mission Street", "San Francisco", "CA 94110"),
                new(37.7609, -122.4350, "Castro & Twin Peaks", "400 Castro Street", "San Francisco", "CA 94114"),
                new(37.8005, -122.4368, "Marina & Presidio", "1800 Chestnut Street", "San Francisco", "CA 94123"),
                new(37.7535, -122.4925, "Sunset District", "1250 9th Avenue", "San Francisco", "CA 94122"),
                new(37.7797, -122.4820, "Outer Richmond", "320 Geary Boulevard", "San Francisco", "CA 94118"),
                new(37.8080, -122.4177, "Fisherman's Wharf", "Pier 39 The Embarcadero", "San Francisco", "CA 94133"),
                new(37.7932, -122.4162, "Nob Hill Central", "1000 California Street", "San Francisco", "CA 94108"),
                new(37.7620, -122.3900, "Dogpatch Waterfront", "2200 3rd Street", "San Francisco", "CA 94107"),
                new(37.7780, -122.4200, "Civic Center / Hayes Valley", "450 Hayes Street", "San Francisco", "CA 94102"),
                new(37.7680, -122.3920, "Mission Bay BioTech", "550 16th Street", "San Francisco", "CA 94158"),
                new(37.7786, -122.3893, "South Beach & Oracle Park", "24 Willie Mays Plaza", "San Francisco", "CA 94107"),
                new(37.7915, -122.4080, "Union Square", "333 Post Street", "San Francisco", "CA 94108"),
                new(37.7955, -122.4070, "Chinatown Central", "700 Grant Avenue", "San Francisco", "CA 94108"),
                new(37.8000, -122.4090, "North Beach", "550 Columbus Avenue", "San Francisco", "CA 94133"),
                new(37.7930, -122.4350, "Pacific Heights", "2400 Broadway Street", "San Francisco", "CA 94115"),
                new(37.4419, -122.1430, "Palo Alto Tech Hub", "250 University Avenue", "Palo Alto", "CA 94301"),
                new(37.3861, -122.0839, "Mountain View Tech Corridor", "1600 Amphitheatre Pkwy", "Mountain View", "CA 94043"),
                new(37.3688, -122.0363, "Sunnyvale & Cupertino", "1 Apple Park Way", "Cupertino", "CA 95014"),
                new(37.3382, -121.8863, "Downtown San Jose", "100 S First Street", "San Jose", "CA 95113"),
                new(37.8044, -122.2712, "Downtown Oakland", "1200 Broadway", "Oakland", "CA 94612")
            }),

        // Mumbai Metro (Lat 18.80 .. 19.45, Lon 72.70 .. 73.20)
        new MetroRegion(
            "Mumbai Metropolitan Region", 18.80, 19.45, 72.70, 73.20,
            new GeoAnchor[]
            {
                new(19.0664, 72.8682, "BKC (Bandra Kurla Complex)", "G-Block, Bandra Kurla Complex Road", "Mumbai", "MH 400051"),
                new(19.0596, 72.8295, "Bandra West", "Hill Road, Near Bandra Station", "Mumbai", "MH 400050"),
                new(18.9256, 72.8242, "Nariman Point", "Marine Drive Financial Center", "Mumbai", "MH 400021"),
                new(18.9067, 72.8147, "Colaba Heritage District", "Shahid Bhagat Singh Road", "Mumbai", "MH 400005"),
                new(18.9322, 72.8315, "Fort / South Mumbai", "DN Road, Near CST Terminus", "Mumbai", "MH 400001"),
                new(19.0016, 72.8306, "Lower Parel Commercial", "Senapati Bapat Marg, One World Center", "Mumbai", "MH 400013"),
                new(19.0150, 72.8170, "Worli Sea Face", "Dr. Annie Besant Road", "Mumbai", "MH 400018"),
                new(19.0205, 72.8427, "Dadar TT Circle", "Dr. Babasaheb Ambedkar Road", "Mumbai", "MH 400014"),
                new(19.0830, 72.8830, "Kurla West", "LBS Marg, Near Phoenix Marketcity", "Mumbai", "MH 400070"),
                new(19.1136, 72.8697, "Andheri East Tech Hub", "MIDC Central Road, Chakala", "Mumbai", "MH 400093"),
                new(19.1363, 72.8277, "Andheri West Lokhandwala", "Link Road, Lokhandwala Complex", "Mumbai", "MH 400053"),
                new(19.1000, 72.8270, "Juhu Beachfront", "Juhu Tara Road", "Mumbai", "MH 400049"),
                new(19.1176, 72.9060, "Powai Cybercity", "Hiranandani Central Avenue", "Mumbai", "MH 400076"),
                new(19.0980, 72.9280, "Vikhroli East", "Godrej One Commercial Hub", "Mumbai", "MH 400079"),
                new(19.1550, 72.8550, "Goregaon East", "Nesco Complex, Western Express Hwy", "Mumbai", "MH 400063"),
                new(19.1860, 72.8340, "Malad West Mindspace", "Mindspace IT Park, Link Road", "Mumbai", "MH 400064"),
                new(19.2307, 72.8567, "Borivali West", "S.V. Road, Near Shimpoli", "Mumbai", "MH 400092"),
                new(19.2183, 72.9781, "Thane West", "Ghodbunder Road Sector 2", "Thane", "MH 400607"),
                new(19.0771, 72.9986, "Navi Mumbai Vashi", "Sector 30A, Near Vashi Station", "Navi Mumbai", "MH 400703"),
                new(19.1579, 72.9984, "Navi Mumbai Airoli", "Mindspace Knowledge Park, Airoli", "Navi Mumbai", "MH 400708")
            }),

        // Pune Metro (Lat 18.35 .. 18.80, Lon 73.65 .. 74.15)
        new MetroRegion(
            "Pune Metropolitan Region", 18.35, 18.80, 73.65, 74.15,
            new GeoAnchor[]
            {
                new(18.5913, 73.7389, "Hinjewadi IT Park Phase 1", "Rajiv Gandhi Infotech Park Main Rd", "Pune", "MH 411057"),
                new(18.5975, 73.7150, "Hinjewadi Phase 3 Megapolis", "Megapolis Circle, Phase 3 Tech Zone", "Pune", "MH 411057"),
                new(18.5987, 73.7661, "Wakad Telecom Corridor", "Dange Chowk Road, Wakad", "Pune", "MH 411057"),
                new(18.5590, 73.7868, "Baner High Street", "Baner-Pashan Link Road", "Pune", "MH 411045"),
                new(18.5740, 73.7720, "Balewadi Sports Corridor", "Balewadi High Street", "Pune", "MH 411045"),
                new(18.5580, 73.8075, "Aundh", "ITI Road, Near Parihar Chowk", "Pune", "MH 411007"),
                new(18.5314, 73.8446, "Shivajinagar Central", "Fergusson College Road", "Pune", "MH 411005"),
                new(18.5362, 73.8940, "Koregaon Park", "North Main Road, Lane 5", "Pune", "MH 411001"),
                new(18.5470, 73.9030, "Kalyani Nagar", "East Avenue, Near Bishop's School", "Pune", "MH 411006"),
                new(18.5679, 73.9143, "Viman Nagar Airport Hub", "Symbiosis Road, Near Phoenix", "Pune", "MH 411014"),
                new(18.5529, 73.9532, "Kharadi EON Free Zone", "EON IT Park Phase 2", "Pune", "MH 411014"),
                new(18.5158, 73.9272, "Magarpatta Cybercity", "Tower 7 Cybercity Circle", "Pune", "MH 411028"),
                new(18.5020, 73.9350, "Hadapsar Industrial", "Solapur Road Industrial Zone", "Pune", "MH 411028"),
                new(18.5074, 73.8077, "Kothrud", "Paud Road, Near Vanaz Metro", "Pune", "MH 411038"),
                new(18.5150, 73.7700, "Bavdhan Tech Node", "NDA-Pashan Road", "Pune", "MH 411021"),
                new(18.5010, 73.8580, "Swargate Transport Hub", "Pune-Satara Road Chowk", "Pune", "MH 411042"),
                new(18.5170, 73.8790, "Camp / MG Road", "Mahatma Gandhi Road", "Pune", "MH 411001"),
                new(18.4550, 73.8580, "Katraj Tech Sector", "Near Bharati Vidyapeeth", "Pune", "MH 411046"),
                new(18.6270, 73.8010, "Pimpri-Chinchwad (PCMC)", "Old Mumbai-Pune Highway", "Pune", "MH 411018"),
                new(18.6550, 73.7710, "Nigdi Pradhikaran", "Sector 24, Spine Road", "Pune", "MH 411044")
            }),

        // Delhi NCR (Lat 28.30 .. 28.95, Lon 76.80 .. 77.60)
        new MetroRegion(
            "Delhi NCR", 28.30, 28.95, 76.80, 77.60,
            new GeoAnchor[]
            {
                new(28.6315, 77.2167, "Connaught Place Central", "Barakhamba Road, Inner Circle", "New Delhi", "DL 110001"),
                new(28.4950, 77.0890, "DLF Cyber City Gurugram", "DLF Phase 2, Building 10 Tower B", "Gurugram", "HR 122002"),
                new(28.4550, 77.0980, "Golf Course Corridor", "One Horizon Center, Sector 43", "Gurugram", "HR 122002"),
                new(28.6250, 77.3650, "Sector 62 Noida IT Zone", "Stellar IT Park, Sector 62", "Noida", "UP 201309"),
                new(28.5700, 77.3210, "Sector 18 Noida Hub", "Atta Market Road, Sector 18", "Noida", "UP 201301"),
                new(28.5490, 77.2520, "Nehru Place IT Market", "International Trade Tower, Nehru Place", "New Delhi", "DL 110019"),
                new(28.5280, 77.2180, "Saket District Centre", "Press Enclave Marg, Saket", "New Delhi", "DL 110017"),
                new(28.5520, 77.1210, "Aerocity Gateway", "Worldmark 1, Northern Access Rd", "New Delhi", "DL 110037"),
                new(28.5520, 77.0580, "Dwarka Sub-City", "Sector 21 Metro Complex", "New Delhi", "DL 110077"),
                new(28.5720, 77.2210, "South Extension / AIIMS", "Ring Road, South Extension Part 1", "New Delhi", "DL 110049"),
                new(28.6510, 77.1900, "Karol Bagh Commercial", "Pusa Road, Near Metro Pillar 110", "New Delhi", "DL 110005"),
                new(28.5350, 77.2730, "Okhla Industrial Area", "Phase 3 Industrial Area, Okhla", "New Delhi", "DL 110020")
            }),

        // Bengaluru (Lat 12.75 .. 13.25, Lon 77.40 .. 77.90)
        new MetroRegion(
            "Bengaluru", 12.75, 13.25, 77.40, 77.90,
            new GeoAnchor[]
            {
                new(12.9352, 77.6245, "Koramangala Tech Hub", "80 Feet Road, 4th Block, Koramangala", "Bengaluru", "KA 560034"),
                new(12.9784, 77.6408, "Indiranagar", "100 Feet Road, HAL 2nd Stage", "Bengaluru", "KA 560038"),
                new(12.9121, 77.6446, "HSR Layout Sector 1", "27th Main Road, Sector 1, HSR", "Bengaluru", "KA 560102"),
                new(12.9856, 77.7377, "Whitefield ITPL Corridor", "ITPL Main Road, International Tech Park", "Bengaluru", "KA 560066"),
                new(12.8452, 77.6602, "Electronic City Phase 1", "Hosur Road, Infosys Gate 1", "Bengaluru", "KA 560100"),
                new(12.9756, 77.6067, "MG Road & Brigade Rd", "Mahatma Gandhi Road, Craig Park", "Bengaluru", "KA 560001"),
                new(12.9260, 77.6762, "Bellandur ORR Tech Corridor", "EcoSpace Business Park, Outer Ring Rd", "Bengaluru", "KA 560103"),
                new(13.0489, 77.6200, "Manyata Embassy Tech Park", "Thanisandra Main Road, Hebbal", "Bengaluru", "KA 560045"),
                new(12.8920, 77.5830, "JP Nagar", "Kanakapura Main Road, 7th Phase", "Bengaluru", "KA 560078"),
                new(12.9560, 77.7010, "Marathahalli Junction", "Varthur Road, Near Multiplex", "Bengaluru", "KA 560037")
            }),

        // Hyderabad (Lat 17.20 .. 17.65, Lon 78.20 .. 78.70)
        new MetroRegion(
            "Hyderabad", 17.20, 17.65, 78.20, 78.70,
            new GeoAnchor[]
            {
                new(17.4504, 78.3808, "HITEC City Cyber Towers", "Cyber Towers Road, Madhapur", "Hyderabad", "TG 500081"),
                new(17.4399, 78.3489, "Gachibowli Financial District", "ISB Road, Nanakramguda", "Hyderabad", "TG 500032"),
                new(17.4485, 78.3908, "Madhapur IT Hub", "Ayyappa Society Main Road", "Hyderabad", "TG 500081"),
                new(17.4620, 78.3610, "Kondapur", "Botanical Garden Road, Kondapur", "Hyderabad", "TG 500084"),
                new(17.4156, 78.4350, "Banjara Hills", "Road No. 12, Banjara Hills", "Hyderabad", "TG 500034"),
                new(17.4320, 78.4070, "Jubilee Hills", "Road No. 36, Jubilee Hills", "Hyderabad", "TG 500033"),
                new(17.4440, 78.4730, "Begumpet Central", "Sardar Patel Road, Begumpet", "Hyderabad", "TG 500016"),
                new(17.3616, 78.4747, "Charminar Heritage Area", "Pathargatti Road, Charminar", "Hyderabad", "TG 500002")
            }),

        // Chennai (Lat 12.85 .. 13.25, Lon 80.05 .. 80.35)
        new MetroRegion(
            "Chennai", 12.85, 13.25, 80.05, 80.35,
            new GeoAnchor[]
            {
                new(12.9890, 80.2470, "OMR IT Expressway / Tidel Park", "Rajiv Gandhi Salai, Taramani", "Chennai", "TN 600113"),
                new(13.0067, 80.2025, "Guindy Industrial Estate", "GST Road, Guindy Estate", "Chennai", "TN 600032"),
                new(13.0418, 80.2341, "T. Nagar Commercial Hub", "Usman Road, T. Nagar", "Chennai", "TN 600017"),
                new(12.9750, 80.2210, "Velachery Hub", "Velachery Bypass Road", "Chennai", "TN 600042"),
                new(13.0850, 80.2100, "Anna Nagar Roundtana", "2nd Avenue, Anna Nagar", "Chennai", "TN 600040"),
                new(13.0010, 80.2560, "Adyar / Besant Nagar", "Sardar Patel Road, Adyar", "Chennai", "TN 600020")
            }),

        // Kolkata (Lat 22.40 .. 22.75, Lon 88.20 .. 88.55)
        new MetroRegion(
            "Kolkata", 22.40, 22.75, 88.20, 88.55,
            new GeoAnchor[]
            {
                new(22.5800, 88.4320, "Salt Lake Sector V IT Zone", "EP Block, Ring Road, Bidhannagar", "Kolkata", "WB 700091"),
                new(22.5920, 88.4680, "New Town Action Area 1", "Major Arterial Road, New Town", "Kolkata", "WB 700156"),
                new(22.5510, 88.3520, "Park Street Commercial", "Mother Teresa Sarani, Park Street", "Kolkata", "WB 700016"),
                new(22.5850, 88.3410, "Howrah Station District", "Station Road, Howrah", "Kolkata", "WB 711101")
            }),

        // Ahmedabad (Lat 22.90 .. 23.25, Lon 72.40 .. 72.80)
        new MetroRegion(
            "Ahmedabad", 22.90, 23.25, 72.40, 72.80,
            new GeoAnchor[]
            {
                new(23.0500, 72.5050, "SG Highway Tech Corridor", "Sarkhej-Gandhinagar Highway, Bodakdev", "Ahmedabad", "GJ 380054"),
                new(23.0120, 72.5100, "Prahlad Nagar Trade Center", "100 Feet Anand Nagar Rd", "Ahmedabad", "GJ 380015"),
                new(23.1610, 72.6840, "GIFT City SEZ", "GIFT Boulevard, GIFT City", "Gandhinagar", "GJ 382355")
            }),

        // New York Metro (Lat 40.45 .. 41.00, Lon -74.30 .. -73.65)
        new MetroRegion(
            "New York", 40.45, 41.00, -74.30, -73.65,
            new GeoAnchor[]
            {
                new(40.7549, -73.9840, "Midtown Manhattan", "350 5th Avenue (Empire State)", "New York", "NY 10118"),
                new(40.7074, -74.0094, "Wall Street Financial District", "11 Wall Street / Broadway", "New York", "NY 10005"),
                new(40.7538, -74.0022, "Hudson Yards Tech Center", "500 West 33rd Street", "New York", "NY 10001"),
                new(40.7660, -73.9770, "Central Park South", "200 Central Park South", "New York", "NY 10019"),
                new(40.7580, -73.9855, "Times Square / Theater District", "1500 Broadway / 43rd St", "New York", "NY 10036"),
                new(40.7033, -73.9890, "DUMBO Tech Sector", "55 Water Street, DUMBO", "Brooklyn", "NY 11201"),
                new(40.7081, -73.9571, "Williamsburg North", "250 Bedford Avenue", "Brooklyn", "NY 11211"),
                new(40.7447, -73.9485, "Long Island City Tech Hub", "1 Court Square", "Queens", "NY 11101"),
                new(40.7736, -73.9566, "Upper East Side", "1000 Madison Avenue", "New York", "NY 10075"),
                new(40.7200, -74.0000, "SoHo Creative District", "450 Broadway", "New York", "NY 10013"),
                new(40.7450, -74.0050, "Chelsea Arts District", "200 10th Avenue", "New York", "NY 10011"),
                new(40.7300, -73.9950, "Greenwich Village", "70 Washington Square South", "New York", "NY 10012"),
                new(40.8100, -73.9450, "Harlem Central", "200 West 125th Street", "New York", "NY 10027")
            }),

        // London Metro (Lat 51.25 .. 51.75, Lon -0.55 .. 0.25)
        new MetroRegion(
            "London", 51.25, 51.75, -0.55, 0.25,
            new GeoAnchor[]
            {
                new(51.5155, -0.0922, "City of London / Square Mile", "100 Bishopsgate", "London", "EC2N 4AG"),
                new(51.5054, -0.0235, "Canary Wharf Financial Hub", "1 Canada Square", "London", "E14 5AB"),
                new(51.4995, -0.1332, "Westminster & Whitehall", "Parliament Square, Westminster", "London", "SW1A 2PW"),
                new(51.5136, -0.1264, "Soho / Oxford Circus", "100 Oxford Street", "London", "W1D 1BS"),
                new(51.5308, -0.1238, "King's Cross Tech Hub", "York Way / Pancras Square", "London", "N1C 4AX"),
                new(51.5235, -0.0772, "Shoreditch Silicon Roundabout", "Great Eastern Street", "London", "EC2A 3NT"),
                new(51.4988, -0.1749, "Kensington High Street", "220 Kensington High Street", "London", "W8 7RG"),
                new(51.4820, -0.0050, "Greenwich Waterfront", "Greenwich High Road", "London", "SE10 8JA"),
                new(51.5400, -0.1420, "Camden Town", "Camden High Street", "London", "NW1 7JE"),
                new(51.5180, -0.1780, "Paddington Basin", "Merchant Square", "London", "W2 1AS"),
                new(51.5050, -0.0860, "London Bridge & Southwark", "More London Riverside", "London", "SE1 2DB")
            }),

        // Tokyo Metro (Lat 35.40 .. 35.95, Lon 139.40 .. 140.05)
        new MetroRegion(
            "Tokyo", 35.40, 35.95, 139.40, 140.05,
            new GeoAnchor[]
            {
                new(35.6595, 139.7004, "Shibuya Crossing", "1-1 Dogenzaka, Shibuya-ku", "Tokyo", "150-0043"),
                new(35.6938, 139.7034, "Shinjuku Skyscraper District", "2-8 Nishi-Shinjuku, Shinjuku-ku", "Tokyo", "160-0023"),
                new(35.6719, 139.7648, "Ginza District", "4-5 Ginza, Chuo-ku", "Tokyo", "104-0061"),
                new(35.7022, 139.7741, "Akihabara Electric Town", "1-1 Soto-Kanda, Chiyoda-ku", "Tokyo", "101-0021"),
                new(35.6628, 139.7314, "Roppongi Hills & Minato", "6-10 Roppongi, Minato-ku", "Tokyo", "106-0032"),
                new(35.6812, 139.7671, "Marunouchi / Tokyo Station", "1-9 Marunouchi, Chiyoda-ku", "Tokyo", "100-0005"),
                new(35.6284, 139.7387, "Shinagawa Tech Sector", "Konan 2-Chome, Minato-ku", "Tokyo", "108-0075"),
                new(35.7300, 139.7120, "Ikebukuro", "Higashi-Ikebukuro, Toshima-ku", "Tokyo", "170-0013"),
                new(35.6290, 139.7760, "Odaiba Waterfront", "Daiba 1-Chome, Minato-ku", "Tokyo", "135-0091")
            }),

        // Paris (Lat 48.75 .. 49.00, Lon 2.15 .. 2.50)
        new MetroRegion(
            "Paris", 48.75, 49.00, 2.15, 2.50,
            new GeoAnchor[]
            {
                new(48.8924, 2.2361, "La Défense Business District", "1 Parvis de la Défense", "Paris", "92800"),
                new(48.8698, 2.3075, "Champs-Élysées", "102 Avenue des Champs-Élysées", "Paris", "75008"),
                new(48.8575, 2.3592, "Le Marais Historic District", "Rue de Rivoli", "Paris", "75004"),
                new(48.8867, 2.3431, "Montmartre", "Place du Tertre", "Paris", "75018"),
                new(48.8462, 2.3444, "Latin Quarter & Sorbonne", "Boulevard Saint-Michel", "Paris", "75005")
            }),

        // Berlin (Lat 52.35 .. 52.65, Lon 13.10 .. 13.70)
        new MetroRegion(
            "Berlin", 52.35, 52.65, 13.10, 13.70,
            new GeoAnchor[]
            {
                new(52.5219, 13.4132, "Alexanderplatz & TV Tower", "Alexanderplatz 1, Mitte", "Berlin", "10178"),
                new(52.5096, 13.3759, "Potsdamer Platz", "Potsdamer Platz 1, Tiergarten", "Berlin", "10785"),
                new(52.4990, 13.4034, "Kreuzberg Tech Hub", "Oranienstraße 25, Kreuzberg", "Berlin", "10961"),
                new(52.5048, 13.3150, "Charlottenburg", "Kurfürstendamm 110", "Berlin", "10707"),
                new(52.5133, 13.4548, "Friedrichshain & Mediaspree", "Warschauer Straße 40", "Berlin", "10243")
            }),

        // Sydney (Lat -34.05 .. -33.65, Lon 150.90 .. 151.35)
        new MetroRegion(
            "Sydney", -34.05, -33.65, 150.90, 151.35,
            new GeoAnchor[]
            {
                new(-33.8614, 151.2108, "Circular Quay & Opera", "1 Macquarie Street", "Sydney", "NSW 2000"),
                new(-33.8732, 151.1994, "Darling Harbour & Barangaroo", "300 Barangaroo Avenue", "Sydney", "NSW 2000"),
                new(-33.8390, 151.2072, "North Sydney CBD", "100 Miller Street", "Sydney", "NSW 2060"),
                new(-33.8150, 151.0011, "Parramatta Tech Precinct", "100 Church Street", "Sydney", "NSW 2150"),
                new(-33.8915, 151.2767, "Bondi Beach Coastal", "Campbell Parade", "Sydney", "NSW 2026")
            }),

        // Dubai (Lat 24.95 .. 25.35, Lon 55.00 .. 55.45)
        new MetroRegion(
            "Dubai", 24.95, 25.35, 55.00, 55.45,
            new GeoAnchor[]
            {
                new(25.1972, 55.2744, "Downtown Dubai & Burj Khalifa", "Sheikh Mohammed bin Rashid Blvd", "Dubai", "Dubai"),
                new(25.0772, 55.1333, "Dubai Marina & JBR", "Marina Promenade, Dubai Marina", "Dubai", "Dubai"),
                new(25.2138, 55.2798, "DIFC Financial Center", "Gate Building, DIFC", "Dubai", "Dubai"),
                new(25.1860, 55.2631, "Business Bay Canal District", "Marasi Drive, Business Bay", "Dubai", "Dubai"),
                new(25.1124, 55.1390, "Palm Jumeirah", "The Crescent, Palm Jumeirah", "Dubai", "Dubai"),
                new(25.2697, 55.3095, "Deira & Dubai Creek", "Baniyas Road, Deira", "Dubai", "Dubai")
            }),

        // Singapore (Lat 1.20 .. 1.48, Lon 103.60 .. 104.05)
        new MetroRegion(
            "Singapore", 1.20, 1.48, 103.60, 104.05,
            new GeoAnchor[]
            {
                new(1.2838, 103.8591, "Marina Bay Financial District", "10 Marina Boulevard, MBFC", "Singapore", "018981"),
                new(1.3048, 103.8318, "Orchard Road Corridor", "230 Orchard Road", "Singapore", "238897"),
                new(1.2995, 103.7874, "One-North Biopolis Tech Hub", "1 Fusionopolis Way", "Singapore", "138632"),
                new(1.3329, 103.7436, "Jurong Innovation District", "Jurong Gateway Road", "Singapore", "609916"),
                new(1.3347, 103.9625, "Changi Business & Aviation Park", "Changi South Avenue 2", "Singapore", "486025")
            })
    };

    // Regional country anchors for locations outside the primary metropolitan regions
    private static readonly GeoAnchor[] BroadNationalAnchors = new[]
    {
        // India regional hubs
        new GeoAnchor(18.5204, 73.8567, "Pune Central", "Shivajinagar Highway Zone", "Pune", "MH 411005"),
        new GeoAnchor(19.0760, 72.8777, "Mumbai Metro Hub", "Eastern Express Highway", "Mumbai", "MH 400071"),
        new GeoAnchor(19.9975, 73.7898, "Nashik Tech Hub", "Trimbak Road", "Nashik", "MH 422002"),
        new GeoAnchor(21.1458, 79.0882, "Nagpur Metro Sector", "Wardha Road IT Park", "Nagpur", "MH 440015"),
        new GeoAnchor(19.8762, 75.3433, "Chhatrapati Sambhaji Nagar", "Jalna Road", "Chhatrapati Sambhaji Nagar", "MH 431001"),
        new GeoAnchor(28.6139, 77.2090, "Delhi National Capital", "Rajpath Avenue Sector", "New Delhi", "DL 110001"),
        new GeoAnchor(12.9716, 77.5946, "Bengaluru South Corridor", "Hosur Main Road", "Bengaluru", "KA 560001"),
        new GeoAnchor(17.3850, 78.4867, "Hyderabad Central Hub", "Inner Ring Road", "Hyderabad", "TG 500001"),
        new GeoAnchor(13.0827, 80.2707, "Chennai Metro Node", "Anna Salai Arterial", "Chennai", "TN 600002"),
        new GeoAnchor(22.5726, 88.3639, "Kolkata Central Zone", "Chittaranjan Avenue", "Kolkata", "WB 700012"),
        new GeoAnchor(23.0225, 72.5714, "Ahmedabad Central", "Ashram Road", "Ahmedabad", "GJ 380009"),
        new GeoAnchor(26.9124, 75.7873, "Jaipur Heritage Sector", "MI Road Commercial Zone", "Jaipur", "RJ 302001"),
        new GeoAnchor(30.7333, 76.7794, "Chandigarh Tricity Node", "Madhya Marg Sector 17", "Chandigarh", "CH 160017"),
        new GeoAnchor(26.8467, 80.9462, "Lucknow Gomti Nagar", "Vibhuti Khand", "Lucknow", "UP 226010"),
        new GeoAnchor(22.7196, 75.8577, "Indore Super Corridor", "AB Road Tech Corridor", "Indore", "MP 452001"),
        new GeoAnchor(9.9312, 76.2673, "Kochi Infopark Hub", "Kakkanad Express Corridor", "Kochi", "KL 682030"),
        new GeoAnchor(17.6868, 83.2185, "Visakhapatnam Coastal Node", "VIP Road", "Visakhapatnam", "AP 530003"),

        // North America
        new GeoAnchor(34.0522, -118.2437, "Downtown Los Angeles", "Grand Avenue", "Los Angeles", "CA 90012"),
        new GeoAnchor(47.6062, -122.3321, "Seattle Tech Corridor", "Westlake Avenue", "Seattle", "WA 98109"),
        new GeoAnchor(41.8781, -87.6298, "Chicago Loop", "Michigan Avenue", "Chicago", "IL 60604"),
        new GeoAnchor(30.2672, -97.7431, "Austin Silicon Hills", "Congress Avenue", "Austin", "TX 78701"),
        new GeoAnchor(43.6532, -79.3832, "Downtown Toronto", "Bay Street Financial District", "Toronto", "ON M5H 2R2"),

        // Europe & Middle East & Asia
        new GeoAnchor(52.3676, 4.9041, "Amsterdam Zuidas", "Gustav Mahlerlaan", "Amsterdam", "1082 MK"),
        new GeoAnchor(50.1109, 8.6821, "Frankfurt Financial", "Mainzer Landstraße", "Frankfurt", "60325"),
        new GeoAnchor(47.3769, 8.5417, "Zurich Tech & Banking", "Bahnhofstrasse", "Zurich", "8001"),
        new GeoAnchor(40.4168, -3.7038, "Madrid Paseo de la Castellana", "Paseo de la Castellana", "Madrid", "28046"),
        new GeoAnchor(41.9028, 12.4964, "Rome Central", "Via del Corso", "Rome", "00187"),
        new GeoAnchor(24.7136, 46.6753, "Riyadh Olaya District", "King Fahd Road", "Riyadh", "12211"),
        new GeoAnchor(37.5665, 126.9780, "Seoul Gangnam Hub", "Teheran-ro", "Seoul", "06236"),
        new GeoAnchor(22.3193, 114.1694, "Hong Kong Central", "Des Voeux Road Central", "Hong Kong", "999077"),
        new GeoAnchor(-37.8136, 144.9631, "Melbourne CBD", "Collins Street", "Melbourne", "VIC 3000")
    };

    public static (string Area, string StreetAddress, string City, string PostalCode) ResolveGeographicAddress(double lat, double lon, int index, string tech)
    {
        // 1. Check if the coordinate falls within any defined high-density metropolitan region
        foreach (var region in DefinedMetroRegions)
        {
            if (lat >= region.MinLat && lat <= region.MaxLat && lon >= region.MinLon && lon <= region.MaxLon)
            {
                // Nearest-Neighbor spatial matching: pick the anchor with the minimum physical distance to (lat, lon)
                GeoAnchor bestAnchor = region.Anchors[0];
                double bestDist = double.MaxValue;

                foreach (var anchor in region.Anchors)
                {
                    double dist = GeodesyUtils.CalculateDistanceMeters(lat, lon, anchor.Lat, anchor.Lon);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestAnchor = anchor;
                    }
                }

                string street;
                if (bestDist < 250.0)
                {
                    street = bestAnchor.Street;
                }
                else
                {
                    string dir = GeodesyUtils.GetCompassDirection(bestAnchor.Lat, bestAnchor.Lon, lat, lon);
                    if (bestDist < 1200.0)
                    {
                        street = $"{dir} Sector, Near {bestAnchor.Street}";
                    }
                    else
                    {
                        street = $"{dir} Sector, {bestAnchor.Area}";
                    }
                }

                return (bestAnchor.Area, street, bestAnchor.City, bestAnchor.PostalCode);
            }
        }

        // 2. Check broader national/regional anchors (nearest-neighbor across world cities)
        GeoAnchor closestBroad = BroadNationalAnchors[0];
        double closestBroadDist = double.MaxValue;

        foreach (var anchor in BroadNationalAnchors)
        {
            double dist = GeodesyUtils.CalculateDistanceMeters(lat, lon, anchor.Lat, anchor.Lon);
            if (dist < closestBroadDist)
            {
                closestBroadDist = dist;
                closestBroad = anchor;
            }
        }

        // If within 150 km of a known major city/anchor, resolve to that city's regional sector
        if (closestBroadDist <= 150000.0)
        {
            string dir = GeodesyUtils.GetCompassDirection(closestBroad.Lat, closestBroad.Lon, lat, lon);
            string area = $"{closestBroad.City} {dir} Regional Sector";
            string street = $"{dir} Bypass Corridor, Near {closestBroad.Area}";
            return (area, street, closestBroad.City, closestBroad.PostalCode);
        }

        // 3. Realistic global geographic sector fallback for arbitrary/remote coordinates
        string ns = lat >= 0 ? "North" : "South";
        string ew = lon >= 0 ? "East" : "West";
        string quad = $"{ns}-{ew} Regional Sector";
        string fallbackStreet = $"Cellular Mast #{index + 101}, Sector Grid Site";
        string fallbackCity = $"Regional Telecom Grid ({Math.Abs(lat):F2}°{(lat >= 0 ? "N" : "S")}, {Math.Abs(lon):F2}°{(lon >= 0 ? "E" : "W")})";
        string zip = $"LOC-{(Math.Abs((int)(lat * 100)) + Math.Abs((int)(lon * 100))):D5}";

        return (quad, fallbackStreet, fallbackCity, zip);
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

using CellScope.Application.DTOs;
using CellScope.Infrastructure.Services;
using Xunit;

namespace CellScope.UnitTests;

public class RadioAnalysisUnitTests
{
    private readonly CellularRadioAnalysisService _radioService = new();
    private readonly Private5gCoreService _private5gService = new();

    [Fact]
    public void CellularRadioAnalysisService_CalculatesCellLoad_Accurately()
    {
        // Light load scenario (RSRQ = -5.0 dB)
        var lightSnapshot = new CellularSnapshotDto
        {
            CellId = "410-01-100200",
            RadioTechnology = "5G NR",
            Band = "n78",
            SignalStrengthDbm = -75,
            SignalQuality = -5.0
        };

        var lightCapacity = _radioService.CalculateCellLoad(lightSnapshot, null);
        Assert.NotNull(lightCapacity);
        Assert.True(lightCapacity.EstimatedLoadPercent < 35.0, "High RSRQ should indicate low cell load.");
        Assert.Equal("Low (Optimal)", lightCapacity.CongestionLevel);
        Assert.True(lightCapacity.ChannelQualityIndicator >= 10);
        Assert.Equal("256-QAM", lightCapacity.ModulationScheme);

        // Congested scenario (RSRQ = -18.0 dB)
        var heavySnapshot = new CellularSnapshotDto
        {
            CellId = "410-01-100200",
            RadioTechnology = "5G NR",
            Band = "n78",
            SignalStrengthDbm = -95,
            SignalQuality = -18.0
        };

        var heavyCapacity = _radioService.CalculateCellLoad(heavySnapshot, null);
        Assert.NotNull(heavyCapacity);
        Assert.True(heavyCapacity.EstimatedLoadPercent >= 80.0, "Low RSRQ should indicate heavy congestion.");
        Assert.Contains("Severe", heavyCapacity.CongestionLevel);
    }

    [Fact]
    public void CellularRadioAnalysisService_DetectsRogueBaseStation_WhenEncryptionDowngraded()
    {
        // Normal 5G secure cell
        var secureSnapshot = new CellularSnapshotDto
        {
            CellId = "410-01-100200",
            RadioTechnology = "5G NR",
            SignalStrengthDbm = -85
        };
        var secureNeighbors = new List<NeighborCellDto>
        {
            new() { CellId = "410-01-100201" },
            new() { CellId = "410-01-100202" }
        };

        var secureThreat = _radioService.AnalyzeCellThreats(secureSnapshot, null, secureNeighbors);
        Assert.NotNull(secureThreat);
        Assert.False(secureThreat.IsRogueBaseStationSuspected);
        Assert.True(secureThreat.ThreatScore < 20);

        // Suspicious cell: 2G forced fallback + 0 neighbor relations
        var rogueSnapshot = new CellularSnapshotDto
        {
            CellId = "999-99-666666",
            RadioTechnology = "2G",
            SignalStrengthDbm = -40 // Suspiciously high power
        };
        var rogueNeighbors = new List<NeighborCellDto>(); // 0 neighbors

        var rogueThreat = _radioService.AnalyzeCellThreats(rogueSnapshot, null, rogueNeighbors);
        Assert.NotNull(rogueThreat);
        Assert.True(rogueThreat.IsRogueBaseStationSuspected, "2G fallback with 0 neighbors should be flagged as suspected rogue base station.");
        Assert.True(rogueThreat.ThreatScore >= 60);
        Assert.False(rogueThreat.IsEncryptionActive);
    }

    [Fact]
    public void CellularRadioAnalysisService_DecodesSibAndChannel_Accurately()
    {
        var snapshot = new CellularSnapshotDto
        {
            CellId = "310-410-582910",
            RadioTechnology = "5G NR",
            Band = "n78",
            SignalStrengthDbm = -82
        };

        var sib = _radioService.DecodeSibAndChannel(snapshot, null);
        Assert.NotNull(sib);
        Assert.Equal("n78", sib.OperatingBand);
        Assert.Equal(3550.0, sib.DownlinkFrequencyMhz);
        Assert.Equal("TDD (Time Division Duplex)", sib.DuplexMode);
        Assert.Equal(100.0, sib.ChannelBandwidthMhz);
        Assert.True(sib.TimingAdvanceDistanceMeters > 0);
        Assert.True(sib.IsCellSelectionCriteriaMet);
        Assert.NotEmpty(sib.DecodedSibBlocks);
    }

    [Fact]
    public void CellularRadioAnalysisService_GeneratesRfPropagationAndSectors_Successfully()
    {
        var tower = new TowerLocationDto
        {
            CellId = "310-410-582910",
            RadioTechnology = "5G NR",
            Latitude = 37.7749,
            Longitude = -122.4194,
            RangeMeters = 2400
        };

        var prop = _radioService.CalculateRfPropagation(tower, 3500.0);
        Assert.NotNull(prop);
        Assert.Equal(3, prop.Sectors.Count);
        Assert.Equal(0.0, prop.Sectors[0].AzimuthDegrees);
        Assert.Equal(120.0, prop.Sectors[1].AzimuthDegrees);
        Assert.Equal(240.0, prop.Sectors[2].AzimuthDegrees);
        Assert.Equal(4, prop.ContourRings.Count);
        Assert.All(prop.Sectors, s => Assert.True(s.PolygonGeoJsonCoordinates.Count > 3));
    }

    [Fact]
    public async Task Private5gCoreService_ReturnsCoreStatusAndSubscribers_Successfully()
    {
        var status = await _private5gService.GetCoreStatusAsync();
        Assert.NotNull(status);
        Assert.True(status.IsConnected);
        Assert.NotEmpty(status.NetworkFunctions);

        var subscribers = await _private5gService.GetConnectedSubscribersAsync();
        Assert.NotEmpty(subscribers);
        Assert.All(subscribers, s =>
        {
            Assert.NotEmpty(s.Supi);
            Assert.NotEmpty(s.AllocatedIpAddress);
            Assert.NotEmpty(s.SstSdSlice);
        });

        var firstSub = await _private5gService.GetSubscriberBySupiAsync(subscribers[0].Supi);
        Assert.NotNull(firstSub);
        Assert.Equal(subscribers[0].AllocatedIpAddress, firstSub.AllocatedIpAddress);
    }
}

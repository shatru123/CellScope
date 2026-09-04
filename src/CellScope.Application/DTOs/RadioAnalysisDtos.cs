namespace CellScope.Application.DTOs;

/// <summary>
/// Real-time Cell Capacity, PRB Utilization & Subscriber Density Estimation.
/// </summary>
public class CellCapacityDto
{
    public string CellId { get; set; } = string.Empty;
    public string RadioTechnology { get; set; } = "5G NR";
    public string Band { get; set; } = "n78";
    public double EstimatedLoadPercent { get; set; } // 0.0 to 100.0%
    public string CongestionLevel { get; set; } = "Low"; // Low, Moderate, High, Severe
    public string CongestionColor { get; set; } = "#10B981";
    public double PrbUtilizationPercent { get; set; }
    public int EstimatedActiveUeDensity { get; set; } // UEs / km²
    public int ChannelQualityIndicator { get; set; } // CQI 1-15
    public string ModulationScheme { get; set; } = "256-QAM";
    public double EstimatedMaxThroughputMbps { get; set; }
    public double EstimatedAvailableThroughputMbps { get; set; }
    public double RsrpDbm { get; set; }
    public double RsrqDb { get; set; }
    public double SinrDb { get; set; }
    public string PerformanceVerdict { get; set; } = string.Empty;
    public List<string> OptimizationRecommendations { get; set; } = new();
}

/// <summary>
/// Rogue Base Station & IMSI-Catcher / Stingray Threat Analysis.
/// </summary>
public class CellThreatAnalysisDto
{
    public string CellId { get; set; } = string.Empty;
    public int ThreatScore { get; set; } // 0 (Safe) to 100 (Critical Threat)
    public string SecurityStatus { get; set; } = "Secure (Verified)"; // Secure, Normal, Suspicious, High Risk
    public string StatusColor { get; set; } = "#10B981";
    public bool IsRogueBaseStationSuspected { get; set; }
    public List<SecurityAnomalyDto> Anomalies { get; set; } = new();
    public string CipheringAlgorithm { get; set; } = "128-NEA2 (AES-CTR)";
    public bool IsEncryptionActive { get; set; } = true;
    public bool IsIntegrityActive { get; set; } = true;
    public int NeighborCellCount { get; set; }
    public bool IsTacConsistent { get; set; } = true;
    public bool IsPowerProfileNormal { get; set; } = true;
    public string DefenseRecommendation { get; set; } = string.Empty;
}

public class SecurityAnomalyDto
{
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low"; // Low, Medium, High, Critical
    public string SeverityColor { get; set; } = "#10B981";
    public string Description { get; set; } = string.Empty;
    public string TechnicalImpact { get; set; } = string.Empty;
    public bool IsTriggered { get; set; }
}

/// <summary>
/// Decoded SIB Broadcast Information & 3GPP Radio Channel Parameters.
/// </summary>
public class SibAnalysisDto
{
    public string CellId { get; set; } = string.Empty;
    public string PhysicalCellId { get; set; } = string.Empty;
    public string RadioTechnology { get; set; } = "5G NR";
    public string PlmnIdentity { get; set; } = "310-410";
    public string OperatorName { get; set; } = "Carrier";
    public string TrackingAreaCode { get; set; } = "14205";
    public long ChannelNumber { get; set; } // EARFCN or NR-ARFCN
    public string ChannelType { get; set; } = "NR-ARFCN";
    public double DownlinkFrequencyMhz { get; set; }
    public double UplinkFrequencyMhz { get; set; }
    public string OperatingBand { get; set; } = "n78";
    public string BandDescription { get; set; } = "3500 MHz (C-Band / Mid-band TDD)";
    public string DuplexMode { get; set; } = "TDD";
    public double ChannelBandwidthMhz { get; set; } = 100.0;
    public int SubcarrierSpacingKhz { get; set; } = 30;
    public int TimingAdvanceSteps { get; set; } = 4;
    public double TimingAdvanceDistanceMeters { get; set; } = 312.5;
    public double PropagationDelayMicroseconds { get; set; } = 1.04;
    public double QRxLevMinDbm { get; set; } = -128.0;
    public double ActualRsrpDbm { get; set; } = -84.0;
    public bool IsCellSelectionCriteriaMet { get; set; } = true;
    public Dictionary<string, string> DecodedSibBlocks { get; set; } = new();
}

/// <summary>
/// RF Signal Propagation, Sector Antennas & Coverage Polygons.
/// </summary>
public class RfPropagationModelDto
{
    public string CellId { get; set; } = string.Empty;
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double AntennaHeightMeters { get; set; } = 35.0;
    public double CarrierFrequencyMhz { get; set; } = 3500.0;
    public double TotalEffectiveIsotropicRadiatedPowerDbm { get; set; } = 46.0; // 40 Watts
    public double MaxCoverageRadiusMeters { get; set; } = 2400.0;
    public double UrbanPathLossExponent { get; set; } = 3.52;
    public List<AntennaSectorDto> Sectors { get; set; } = new();
    public List<SignalContourRingDto> ContourRings { get; set; } = new();
}

public class AntennaSectorDto
{
    public int SectorId { get; set; } // 1, 2, 3 (Alpha, Beta, Gamma)
    public string SectorName { get; set; } = "Sector Alpha (0° North)";
    public double AzimuthDegrees { get; set; } // 0, 120, 240
    public double HorizontalBeamwidthDegrees { get; set; } = 65.0;
    public double ElectricalDowntiltDegrees { get; set; } = 4.0;
    public double MechanicalDowntiltDegrees { get; set; } = 2.0;
    public double MainLobeGainDbi { get; set; } = 18.0;
    public List<double[]> PolygonGeoJsonCoordinates { get; set; } = new(); // [lat, lon] pairs
}

public class SignalContourRingDto
{
    public string Rating { get; set; } = "Excellent";
    public double RsrpThresholdDbm { get; set; } // -80, -95, -110, -125
    public double RadiusMeters { get; set; }
    public string ColorHex { get; set; } = "#10B981";
    public double FillOpacity { get; set; } = 0.25;
}

/// <summary>
/// Private 5G / O-RAN Core Integration Models.
/// </summary>
public class Private5gCoreStatusDto
{
    public string CoreName { get; set; } = "Open5GS 5G Standalone (SA)";
    public string CoreVersion { get; set; } = "v2.7.2";
    public string EndpointUrl { get; set; } = "http://127.0.0.1:9999";
    public bool IsConnected { get; set; } = true;
    public string Plmn { get; set; } = "999-70 (Private 5G)";
    public int ActiveGNodeBCount { get; set; } = 3;
    public int TotalRegisteredSubscribers { get; set; } = 18;
    public int ActivePduSessions { get; set; } = 15;
    public double AggregateThroughputMbps { get; set; } = 485.6;
    public List<NetworkFunctionHealthDto> NetworkFunctions { get; set; } = new();
    public DateTimeOffset LastPolledAt { get; set; } = DateTimeOffset.UtcNow;
}

public class NetworkFunctionHealthDto
{
    public string Name { get; set; } = "AMF";
    public string Role { get; set; } = "Access and Mobility Management";
    public string Status { get; set; } = "Healthy";
    public string StatusColor { get; set; } = "#10B981";
    public string IpAddress { get; set; } = "127.0.0.5:38412";
    public long ProcessedMessagesCount { get; set; } = 45210;
}

public class Private5gSubscriberDto
{
    public string Supi { get; set; } = "imsi-999700000000001";
    public string Guti { get; set; } = "999-70-01-0001-000001";
    public string AllocatedIpAddress { get; set; } = "10.45.0.2";
    public string DeviceType { get; set; } = "Industrial AGV Robotics";
    public string SstSdSlice { get; set; } = "SST: 1 (eMBB) / SD: 0x000001";
    public int Qfi5Qi { get; set; } = 5; // 5QI 5 (Mission Critical IMS)
    public int PduSessionId { get; set; } = 1;
    public string GNodeBId { get; set; } = "gNB-001 (Main Factory)";
    public double DownlinkRateMbps { get; set; } = 84.5;
    public double UplinkRateMbps { get; set; } = 32.1;
    public double PingLatencyMs { get; set; } = 8.2;
    public int SignalRsrpDbm { get; set; } = -78;
    public string ConnectionState { get; set; } = "RRC_CONNECTED";
    public DateTimeOffset ConnectedAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(-42);
}

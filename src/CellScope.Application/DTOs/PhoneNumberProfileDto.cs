namespace CellScope.Application.DTOs;

public class PhoneNumberProfileDto
{
    public string InputNumber { get; set; } = string.Empty;
    public string E164Number { get; set; } = string.Empty;
    public string NationalNumber { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string DialCode { get; set; } = string.Empty;
    public string CountryFlag { get; set; } = "🌐";
    
    public string TelecomCircle { get; set; } = string.Empty;
    public string OriginalCarrier { get; set; } = string.Empty;
    public string LineType { get; set; } = "Mobile";
    public bool IsVoip { get; set; } = false;
    public bool IsValid { get; set; } = true;
    
    public string RiskLevel { get; set; } = "Low";
    public int RiskScore { get; set; } = 12;
    public List<string> RiskFactors { get; set; } = new();
    
    public string? MccMncHint { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public string ConsensualTrackingUrl { get; set; } = string.Empty;
    public DateTimeOffset AnalyzedAt { get; set; } = DateTimeOffset.UtcNow;

    // Live Cellular Sector Attachment (if registered in current network deployment)
    public bool IsAttachedToNetwork { get; set; } = false;
    public string? ServingTowerName { get; set; }
    public string? ServingCellId { get; set; }
    public double? ServingLatitude { get; set; }
    public double? ServingLongitude { get; set; }
    public string? ServingArea { get; set; }
    public string? ServingTechnology { get; set; }
    public string? ServingBand { get; set; }
    public int? ServingSignalDbm { get; set; }
    public string? MatchedDeviceName { get; set; }
}

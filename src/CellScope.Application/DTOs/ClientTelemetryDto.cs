namespace CellScope.Application.DTOs;

public class ClientTelemetryDto
{
    public double? DownlinkMbps { get; set; }
    public int? RttMs { get; set; }
    public string? EffectiveType { get; set; } = "4g";
    public bool? SaveData { get; set; } = false;
    public int? BatteryLevelPercent { get; set; }
    public bool? IsCharging { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

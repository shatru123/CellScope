namespace CellScope.Application.DTOs;

public class SpeedBenchmarkResultDto
{
    public double PingMinMs { get; set; }
    public double PingMaxMs { get; set; }
    public double PingAvgMs { get; set; }
    public double JitterMs { get; set; }
    public double DownloadSpeedMbps { get; set; }
    public double UploadSpeedMbps { get; set; }
    public string BufferbloatGrade { get; set; } = "A";
    public string ConnectionQualityRating { get; set; } = "Excellent";
    public string ServerLocation { get; set; } = "Nearest Edge Point of Presence";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

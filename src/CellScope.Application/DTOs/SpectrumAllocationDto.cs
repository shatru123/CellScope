namespace CellScope.Application.DTOs;

public class SpectrumAllocationDto
{
    public string BandNumber { get; set; } = string.Empty;
    public string BandName { get; set; } = string.Empty;
    public string FrequencyRange { get; set; } = string.Empty;
    public string DuplexMode { get; set; } = "FDD";
    public string TypicalBandwidthsMhz { get; set; } = string.Empty;
    public string Generation { get; set; } = "5G NR";
    public string PrimaryUse { get; set; } = string.Empty;
    public Dictionary<string, List<string>> CircleHoldings { get; set; } = new();
    public List<string> KeyOperators { get; set; } = new();
    public string TechnicalDescription { get; set; } = string.Empty;
}

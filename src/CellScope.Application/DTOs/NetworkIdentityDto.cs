namespace CellScope.Application.DTOs;

public class NetworkIdentityDto
{
    public string PublicIp { get; set; } = string.Empty;
    public string IspName { get; set; } = "Cellular / Broadband Carrier";
    public string Organization { get; set; } = string.Empty;
    public string AsNumber { get; set; } = string.Empty;
    public string AsName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public bool IsVpnOrProxy { get; set; } = false;
    public DateTimeOffset RetrievedAt { get; set; } = DateTimeOffset.UtcNow;
}

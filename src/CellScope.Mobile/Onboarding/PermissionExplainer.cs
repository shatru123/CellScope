namespace CellScope.Mobile.Onboarding;

public class PermissionRequirement
{
    public string Title { get; set; } = string.Empty;
    public string AndroidPermission { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string FallbackBehavior { get; set; } = string.Empty;
}

public static class AndroidPermissionGuide
{
    public static IReadOnlyList<PermissionRequirement> GetRequiredPermissions() => new List<PermissionRequirement>
    {
        new()
        {
            Title = "Location Access (Fine GPS)",
            AndroidPermission = "android.permission.ACCESS_FINE_LOCATION",
            Purpose = "Required by Android OS to read cellular CellId/PCI/TAC and map your observations on the GIS coverage map.",
            FallbackBehavior = "If denied, cellular measurements are recorded without geographic coordinates."
        },
        new()
        {
            Title = "Phone State & Telephony",
            AndroidPermission = "android.permission.READ_PHONE_STATE",
            Purpose = "Required to read carrier name, MCC/MNC, and SIM registration state from TelephonyManager.",
            FallbackBehavior = "If denied, carrier details will show as 'Restricted on this device'."
        },
        new()
        {
            Title = "Foreground Service",
            AndroidPermission = "android.permission.FOREGROUND_SERVICE",
            Purpose = "Allows reliable background telemetry collection during walks or drives with persistent status notification.",
            FallbackBehavior = "If denied, collection only occurs while the app is actively on screen."
        }
    };
}

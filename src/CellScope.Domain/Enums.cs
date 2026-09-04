namespace CellScope.Domain.Enums;

public enum RadioTechnologyType
{
    Unknown = 0,
    GSM = 1,
    CDMA = 2,
    WCDMA = 3,
    LTE = 4,
    NR5G = 5,
    WiFi = 6,
    Ethernet = 7
}

public enum SignalQualityRating
{
    Unavailable = 0,
    Poor = 1,
    Fair = 2,
    Good = 3,
    Excellent = 4
}

public enum DataAvailability
{
    Available = 0,
    Unavailable = 1,
    Restricted = 2,
    Unknown = 3
}

public enum TowerConfidence
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum NetworkDeviceType
{
    Unknown = 0,
    Router = 1,
    Laptop = 2,
    Desktop = 3,
    Phone = 4,
    Tablet = 5,
    TV = 6,
    IoT = 7,
    AccessPoint = 8,
    Printer = 9,
    Server = 10
}

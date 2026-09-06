using CellScope.Domain.Entities;
using CellScope.Domain.Enums;

namespace CellScope.Domain.Services;

public static class SignalClassifier
{
    /// <summary>
    /// Classifies cellular signal strength (dBm) into standard qualitative ratings.
    /// Default standard ranges:
    /// Excellent: >= -70 dBm
    /// Good: -70 to -85 dBm
    /// Fair: -85 to -100 dBm
    /// Poor: < -100 dBm
    /// </summary>
    public static SignalQualityRating Classify(int? signalStrengthDbm, string? technology = null)
    {
        if (!signalStrengthDbm.HasValue)
            return SignalQualityRating.Unavailable;

        int dbm = signalStrengthDbm.Value;

        // 5G NR SS-RSRP typical scale
        if (!string.IsNullOrEmpty(technology) && technology.Contains("5G", StringComparison.OrdinalIgnoreCase))
        {
            if (dbm >= -80) return SignalQualityRating.Excellent;
            if (dbm >= -95) return SignalQualityRating.Good;
            if (dbm >= -110) return SignalQualityRating.Fair;
            return SignalQualityRating.Poor;
        }

        // LTE RSRP / standard scale
        if (dbm >= -70) return SignalQualityRating.Excellent;
        if (dbm >= -85) return SignalQualityRating.Good;
        if (dbm >= -100) return SignalQualityRating.Fair;
        return SignalQualityRating.Poor;
    }

    public static string GetRatingText(SignalQualityRating rating) => rating switch
    {
        SignalQualityRating.Excellent => "Excellent",
        SignalQualityRating.Good => "Good",
        SignalQualityRating.Fair => "Fair",
        SignalQualityRating.Poor => "Poor",
        _ => "Unavailable"
    };

    public static string GetRatingColor(SignalQualityRating rating) => rating switch
    {
        SignalQualityRating.Excellent => "#10B981", // Emerald green
        SignalQualityRating.Good => "#06B6D4",      // Cyan
        SignalQualityRating.Fair => "#F59E0B",      // Amber
        SignalQualityRating.Poor => "#EF4444",      // Rose red
        _ => "#6B7280"                              // Gray
    };

    public static int GetSignalPercentage(int? signalStrengthDbm)
    {
        if (!signalStrengthDbm.HasValue) return 0;
        int dbm = signalStrengthDbm.Value;
        // Map [-120 dBm, -50 dBm] -> [0%, 100%]
        if (dbm <= -120) return 0;
        if (dbm >= -50) return 100;
        return (int)Math.Round((dbm + 120.0) / 70.0 * 100.0);
    }
}

public static class GeodesyUtils
{
    private const double EarthRadiusMeters = 6371000.0;

    /// <summary>
    /// Calculates great-circle distance between two coordinates in meters using the Haversine formula.
    /// </summary>
    public static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    public static (double minLat, double maxLat, double minLon, double maxLon) GetBoundingBox(double lat, double lon, double radiusMeters)
    {
        double latDelta = (radiusMeters / EarthRadiusMeters) * (180.0 / Math.PI);
        double lonDelta = (radiusMeters / (EarthRadiusMeters * Math.Cos(ToRadians(lat)))) * (180.0 / Math.PI);

        return (lat - latDelta, lat + latDelta, lon - lonDelta, lon + lonDelta);
    }

    /// <summary>
    /// Computes destination coordinates given a starting coordinate, distance in meters, and bearing in degrees.
    /// </summary>
    public static (double Latitude, double Longitude) CalculateOffsetCoordinates(double lat, double lon, double distanceMeters, double bearingDegrees)
    {
        double angularDistance = distanceMeters / EarthRadiusMeters;
        double bearingRad = ToRadians(bearingDegrees);
        double latRad = ToRadians(lat);
        double lonRad = ToRadians(lon);

        double outLatRad = Math.Asin(
            Math.Sin(latRad) * Math.Cos(angularDistance) +
            Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

        double outLonRad = lonRad + Math.Atan2(
            Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
            Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(outLatRad));

        double outLat = outLatRad * (180.0 / Math.PI);
        double outLon = ((outLonRad * (180.0 / Math.PI) + 540.0) % 360.0) - 180.0;
        return (outLat, outLon);
    }

    /// <summary>
    /// Computes the initial bearing from start coordinate to end coordinate in degrees (0..360).
    /// </summary>
    public static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = ToRadians(lon2 - lon1);
        double lat1Rad = ToRadians(lat1);
        double lat2Rad = ToRadians(lat2);
        double y = Math.Sin(dLon) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);
        double bearingRad = Math.Atan2(y, x);
        return (bearingRad * (180.0 / Math.PI) + 360.0) % 360.0;
    }

    /// <summary>
    /// Returns 8-point compass direction (e.g. North, North-East, East) from start coordinate to end coordinate.
    /// </summary>
    public static string GetCompassDirection(double lat1, double lon1, double lat2, double lon2)
    {
        double bearing = CalculateBearing(lat1, lon1, lat2, lon2);
        string[] directions = { "North", "North-East", "East", "South-East", "South", "South-West", "West", "North-West" };
        int idx = (int)Math.Round(bearing / 45.0) % 8;
        return directions[idx];
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);
}

public static class HandoverDetector
{
    public static CellHandover? CheckForHandover(CellularSnapshot previous, CellularSnapshot current)
    {
        if (string.IsNullOrWhiteSpace(previous.CellId) || string.IsNullOrWhiteSpace(current.CellId))
            return null;

        bool cellChanged = !string.Equals(previous.CellId, current.CellId, StringComparison.OrdinalIgnoreCase);
        bool techChanged = !string.Equals(previous.RadioTechnology, current.RadioTechnology, StringComparison.OrdinalIgnoreCase);

        if (cellChanged || techChanged)
        {
            string reason = cellChanged && techChanged
                ? "Serving cell and radio technology handover"
                : (cellChanged ? "Serving cell handover" : "Radio technology reselection");

            return new CellHandover
            {
                DeviceId = current.DeviceId,
                Timestamp = current.Timestamp,
                PreviousCellId = previous.CellId,
                NewCellId = current.CellId,
                PreviousRadioTechnology = previous.RadioTechnology,
                NewRadioTechnology = current.RadioTechnology,
                PreviousSignalDbm = previous.SignalStrengthDbm,
                NewSignalDbm = current.SignalStrengthDbm,
                Latitude = current.Latitude,
                Longitude = current.Longitude,
                TriggerReason = reason
            };
        }

        return null;
    }
}

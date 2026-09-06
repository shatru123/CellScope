using System.Text;
using System.Text.Json;
using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;

namespace CellScope.Infrastructure.Services;

public class GisExportService : IGisExportService
{
    public string GenerateGeoJson(IReadOnlyList<TowerLocationDto> towers, CellularSnapshotDto? servingCell, IReadOnlyList<LocationPointDto>? trail)
    {
        var features = new List<object>();

        // 1. Tower Points
        foreach (var t in towers)
        {
            features.Add(new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Point",
                    coordinates = new[] { t.Longitude, t.Latitude, 35.0 } // 35m AGL (Above Ground Level)
                },
                properties = new Dictionary<string, object?>
                {
                    ["cellId"] = t.CellId,
                    ["operator"] = t.OperatorName,
                    ["technology"] = t.RadioTechnology,
                    ["pci"] = t.PhysicalCellId,
                    ["area"] = t.Area,
                    ["address"] = $"{t.StreetAddress}, {t.City} {t.PostalCode}",
                    ["rangeMeters"] = t.RangeMeters,
                    ["connectedDevices"] = t.TotalConnectedDevices,
                    ["confidence"] = t.Confidence.ToString(),
                    ["source"] = t.Source
                }
            });
        }

        // 2. Serving Cell Point
        if (servingCell?.Latitude != null && servingCell.Longitude != null)
        {
            features.Add(new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Point",
                    coordinates = new[] { servingCell.Longitude.Value, servingCell.Latitude.Value, 40.0 }
                },
                properties = new Dictionary<string, object?>
                {
                    ["cellId"] = servingCell.CellId,
                    ["type"] = "ServingCell",
                    ["technology"] = servingCell.RadioTechnology,
                    ["band"] = servingCell.Band,
                    ["signalStrengthDbm"] = servingCell.SignalStrengthDbm,
                    ["signalQuality"] = servingCell.SignalQuality,
                    ["isServing"] = true
                }
            });
        }

        // 3. Mobility Trail LineString
        if (trail != null && trail.Count > 1)
        {
            var coords = trail.Select(p => new[] { p.Longitude, p.Latitude }).ToList();
            features.Add(new
            {
                type = "Feature",
                geometry = new
                {
                    type = "LineString",
                    coordinates = coords
                },
                properties = new Dictionary<string, object?>
                {
                    ["type"] = "DeviceMobilityTrail",
                    ["pointCount"] = trail.Count,
                    ["recordedAt"] = DateTimeOffset.UtcNow
                }
            });
        }

        var geoJsonDoc = new
        {
            type = "FeatureCollection",
            generator = "CellScope Network Intelligence Engine",
            timestamp = DateTimeOffset.UtcNow,
            features = features
        };

        return JsonSerializer.Serialize(geoJsonDoc, new JsonSerializerOptions { WriteIndented = true });
    }

    public string GenerateKml(IReadOnlyList<TowerLocationDto> towers, CellularSnapshotDto? servingCell, IReadOnlyList<LocationPointDto>? trail)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<kml xmlns="http://www.opengis.net/kml/2.2">""");
        sb.AppendLine("  <Document>");
        sb.AppendLine("    <name>CellScope Cellular GIS Export</name>");
        sb.AppendLine("    <description>3D Base Station Infrastructure &amp; Coverage Extrusions</description>");

        // Styles
        sb.AppendLine("""
            <Style id="tower5g">
              <IconStyle>
                <color>ff00d7ff</color>
                <scale>1.2</scale>
                <Icon><href>http://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon>
              </IconStyle>
              <LineStyle><color>ff00d7ff</color><width>2</width></LineStyle>
            </Style>
            <Style id="towerLte">
              <IconStyle>
                <color>ff00aaff</color>
                <scale>1.1</scale>
                <Icon><href>http://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon>
              </IconStyle>
              <LineStyle><color>ff00aaff</color><width>2</width></LineStyle>
            </Style>
            <Style id="trailStyle">
              <LineStyle><color>ff00ffff</color><width>3</width></LineStyle>
            </Style>
        """);

        // Towers
        foreach (var t in towers)
        {
            string style = t.RadioTechnology.Contains("5G") ? "#tower5g" : "#towerLte";
            sb.AppendLine("    <Placemark>");
            sb.AppendLine($"      <name>{System.Security.SecurityElement.Escape(t.OperatorName ?? "Cellular Base Station")}</name>");
            sb.AppendLine($"      <description><![CDATA[<b>Cell ID:</b> {t.CellId}<br/><b>Tech:</b> {t.RadioTechnology}<br/><b>PCI:</b> {t.PhysicalCellId}<br/><b>Area:</b> {t.Area}<br/><b>Address:</b> {t.StreetAddress}, {t.City} {t.PostalCode}<br/><b>Range:</b> {t.RangeMeters}m]]></description>");
            sb.AppendLine($"      <styleUrl>{style}</styleUrl>");
            sb.AppendLine("      <Point>");
            sb.AppendLine("        <extrude>1</extrude>");
            sb.AppendLine("        <altitudeMode>relativeToGround</altitudeMode>");
            sb.AppendLine($"        <coordinates>{t.Longitude},{t.Latitude},45</coordinates>");
            sb.AppendLine("      </Point>");
            sb.AppendLine("    </Placemark>");
        }

        // Trail
        if (trail != null && trail.Count > 1)
        {
            sb.AppendLine("    <Placemark>");
            sb.AppendLine("      <name>CellScope Mobility Trail</name>");
            sb.AppendLine("      <styleUrl>#trailStyle</styleUrl>");
            sb.AppendLine("      <LineString>");
            sb.AppendLine("        <tessellate>1</tessellate>");
            sb.AppendLine("        <coordinates>");
            foreach (var p in trail)
            {
                sb.AppendLine($"          {p.Longitude},{p.Latitude},5");
            }
            sb.AppendLine("        </coordinates>");
            sb.AppendLine("      </LineString>");
            sb.AppendLine("    </Placemark>");
        }

        sb.AppendLine("  </Document>");
        sb.AppendLine("</kml>");
        return sb.ToString();
    }

    public string GenerateCsv(IReadOnlyList<TowerLocationDto> towers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CellId,Operator,RadioTechnology,Latitude,Longitude,Area,StreetAddress,City,PostalCode,PCI,RangeMeters,Confidence,Source");

        foreach (var t in towers)
        {
            string Escape(string? s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";
            sb.AppendLine($"{Escape(t.CellId)},{Escape(t.OperatorName)},{Escape(t.RadioTechnology)},{t.Latitude},{t.Longitude},{Escape(t.Area)},{Escape(t.StreetAddress)},{Escape(t.City)},{Escape(t.PostalCode)},{Escape(t.PhysicalCellId)},{t.RangeMeters},{Escape(t.Confidence.ToString())},{Escape(t.Source)}");
        }

        return sb.ToString();
    }
}

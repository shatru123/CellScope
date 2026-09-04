using System;
using CellScope.Application.DTOs;
using CellScope.Desktop.Services;

namespace CellScope.Desktop;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
┌─────────────────────────────────────────────────────────────┐
│                       CellScope Desktop                     │
│               Cellular & Network Intelligence               │
│                 'See your cellular world.'                  │
└─────────────────────────────────────────────────────────────┘");
        Console.ResetColor();

        Console.WriteLine("Platform: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        Console.WriteLine("Cellular Telemetry Status: [Direct Modem Unavailable on Desktop]");
        Console.WriteLine("Streaming Telemetry from Android Collector over SignalR & REST API...\n");

        var client = new CellScopeApiClient("http://localhost:5000");
        
        client.OnSnapshotReceived += (snapshot) =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[LIVE TELEMETRY] {snapshot.Timestamp:HH:mm:ss} | {snapshot.OperatorName} | {snapshot.RadioTechnology} | Cell: {snapshot.CellId} | Signal: {snapshot.SignalStrengthDbm} dBm ({snapshot.SignalRating})");
            Console.ResetColor();
        };

        client.OnHandoverReceived += (handover) =>
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            int delta = (handover.NewSignalDbm ?? 0) - (handover.PreviousSignalDbm ?? 0);
            Console.WriteLine($"[HANDOVER EVENT] {handover.Timestamp:HH:mm:ss} | {handover.PreviousCellId} ➔ {handover.NewCellId} | Delta: {delta} dB | Reason: {handover.TriggerReason}");
            Console.ResetColor();
        };

        bool connected = await client.InitializeSignalRAsync();
        Console.WriteLine($"SignalR Real-Time Stream Status: {(connected ? "● Connected" : "○ Disconnected (Start API to stream)")}");

        var snapshot = await client.GetCurrentSnapshotAsync();
        if (snapshot != null)
        {
            Console.WriteLine("\n--- Current Network Observation ---");
            Console.WriteLine($"Operator:      {snapshot.OperatorName}");
            Console.WriteLine($"Technology:    {snapshot.RadioTechnology}");
            Console.WriteLine($"Signal:        {snapshot.SignalStrengthDbm} dBm ({snapshot.SignalRating})");
            Console.WriteLine($"Serving Cell:  {snapshot.CellId} (PCI: {snapshot.PhysicalCellId}, TAC: {snapshot.TrackingAreaCode})");
            Console.WriteLine($"Frequency:     {snapshot.Frequency} ({snapshot.Band})");
            Console.WriteLine($"Location:      {(snapshot.Latitude.HasValue && snapshot.Longitude.HasValue ? $"{snapshot.Latitude.Value:F4}, {snapshot.Longitude.Value:F4}" : "Unavailable")}");
        }

        Console.WriteLine("\nScanning Local Network for authorized devices...");
        var network = await client.ScanLocalNetworkAsync();
        if (network != null && network.Devices.Count > 0)
        {
            Console.WriteLine($"Subnet: {network.Subnet} | Gateway: {network.GatewayIp} | Total Devices: {network.TotalDevices}\n");
            Console.WriteLine($"{"IP Address",-16} {"MAC Address",-18} {"Vendor",-24} {"Type",-10} {"Status",-8}");
            Console.WriteLine(new string('-', 80));
            foreach (var dev in network.Devices)
            {
                Console.WriteLine($"{dev.IpAddress,-16} {dev.MacAddress ?? "Restricted",-18} {dev.Vendor,-24} {dev.DeviceType,-10} {(dev.IsOnline ? "Online" : "Offline"),-8}");
            }
        }

        Console.WriteLine("\nCellScope Desktop Engine initialized.");
    }
}

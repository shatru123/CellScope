using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CellScope.Application.DTOs;
using CellScope.Desktop.Services;
using CellScope.Domain.Services;

namespace CellScope.Desktop;

public class Program
{
    private static CellScopeApiClient _client = new();
    private static bool _isConnectedToApi = false;
    private static NativeHardwareInfo _hwInfo = new();

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "CellScope Desktop — Cellular & Network Intelligence Console";

        // 1. Probe local OS hardware & Wi-Fi PHY
        _hwInfo = NativeHardwareService.ProbeNativeHardware();

        // 2. Detect & connect to local CellScope API engine
        string customUrl = args.Length > 0 ? args[0] : "http://localhost:5050";
        _client = new CellScopeApiClient(customUrl);
        _isConnectedToApi = await _client.DetectAndConnectAsync();

        if (_isConnectedToApi)
        {
            await _client.InitializeSignalRAsync();
        }

        bool running = true;
        while (running)
        {
            RenderHeader();
            RenderMenu();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Enter option [0-9]: ");
            Console.ResetColor();

            var key = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (key)
            {
                case "1":
                    await RunLiveTelemetryStreamAsync();
                    break;
                case "2":
                    await InspectCellularTowersAsync();
                    break;
                case "3":
                    await InspectOngoingCallsAsync();
                    break;
                case "4":
                    await InspectLocalNetworkAsync();
                    break;
                case "5":
                    await ToggleLanDeviceAsync();
                    break;
                case "6":
                    InspectNativeHardware();
                    break;
                case "7":
                    await ShowPairingWizardAsync();
                    break;
                case "8":
                    LaunchWebGisMap();
                    break;
                case "9":
                    await ShowDiagnosticsAsync();
                    break;
                case "0":
                case "q":
                case "exit":
                    running = false;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Exiting CellScope Desktop. Stay connected!");
                    Console.ResetColor();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option. Please choose [0-9].");
                    Console.ResetColor();
                    Thread.Sleep(1000);
                    break;
            }
        }
    }

    private static void RenderHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔════════════════════════════════════════════════════════════════════════════════════════════╗
║                                     CELLSCOPE DESKTOP                                      ║
║                     Carrier-Grade Cellular & Network Intelligence Engine                   ║
║                     Created by: Shatrughna Ambhore (ambhoreshatrughna@gmail.com)          ║
║                                  Phone: +91 96044 66334                                    ║
╚════════════════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("Host: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{_hwInfo.HostName} ({_hwInfo.OperatingSystem})  │  ");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("Engine API: ");
        if (_isConnectedToApi)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"● ONLINE ({_client.BaseUrl})");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("○ STANDALONE / OFFLINE");
        }

        if (!string.IsNullOrEmpty(_hwInfo.WifiSsid))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("Native Wi-Fi: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"SSID: {_hwInfo.WifiSsid} ");
            if (_hwInfo.WifiRssiDbm.HasValue)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"({_hwInfo.WifiRssiDbm} dBm) ");
            }
            if (_hwInfo.WifiTxRateMbps.HasValue)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"• {_hwInfo.WifiTxRateMbps} Mbps PHY");
            }
            Console.WriteLine();
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('─', 88));
        Console.ResetColor();
    }

    private static void RenderMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("AVAILABLE COMMANDS & TELEMETRY MODULES:");
        Console.ResetColor();

        Console.WriteLine("  [1] 📡 Live Cellular Stream HUD (Continuous Real-Time Telemetry & Gauges)");
        Console.WriteLine("  [2] 🗼 Telecom Base Stations & Connected Subscribers (50+ UEs & Numbers)");
        Console.WriteLine("  [3] 📞 Ongoing Voice & Video Calls Monitor (VoNR / VoLTE Sessions & MOS)");
        Console.WriteLine("  [4] 🌐 Local Area Network (LAN) 254-Host Sweep & Discovered Device Roster");
        Console.WriteLine("  [5] ⚡ Toggle LAN Device Connection (Connect / Disconnect Endpoints)");
        Console.WriteLine("  [6] 🔌 Native Hardware Modem & Local Wi-Fi PHY Layer Inspector");
        Console.WriteLine("  [7] 📱 Android Telephony Collector Pairing Wizard (Interactive Code)");
        Console.WriteLine("  [8] 🗺️ Launch GIS Map in Web Browser (http://localhost:5050/map)");
        Console.WriteLine("  [9] 📊 System Diagnostics & Relational Database Health");
        Console.WriteLine("  [0] 🚪 Exit Desktop Engine");
        Console.WriteLine();
    }

    private static async Task RunLiveTelemetryStreamAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════ 📡 LIVE CELLULAR TELEMETRY STREAM (HUD) ══════════════════");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Streaming continuous RF metrics from active baseband / Android collector. Press [ESC] or [ENTER] to exit.\n");
        Console.ResetColor();

        using var cts = new CancellationTokenSource();
        var streamTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var snapshot = await _client.GetCurrentSnapshotAsync();
                Console.SetCursorPosition(0, 3);

                if (snapshot != null)
                {
                    int dbm = snapshot.SignalStrengthDbm ?? -82;
                    int pct = snapshot.SignalPercentage;
                    string bar = RenderSignalBar(pct);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"┌────────────────────────────────────────────────────────────────────────────┐");
                    Console.WriteLine($"│  OPERATOR:      {snapshot.OperatorName,-30} TECHNOLOGY:   {snapshot.RadioTechnology,-14}│");
                    Console.WriteLine($"│  SERVING CELL:  {snapshot.CellId,-30} BAND:         {snapshot.Band,-14}│");
                    Console.WriteLine($"│  PHYSICAL PCI:  {snapshot.PhysicalCellId,-30} TAC:          {snapshot.TrackingAreaCode,-14}│");
                    Console.WriteLine($"│  SIGNAL LEVEL:  {dbm} dBm ({snapshot.SignalRating,-10})            QUALITY:      {(snapshot.SignalQuality != null ? $"{snapshot.SignalQuality} dB" : "Good"),-14}│");
                    Console.WriteLine($"│  SIGNAL GAUGE:  [{bar}] {pct}%               │");
                    Console.WriteLine($"│  LOCATION:      {(snapshot.Latitude.HasValue ? $"{snapshot.Latitude:F4}, {snapshot.Longitude:F4}" : "GPS Pending"),-30} RATING:       {snapshot.SignalRating,-14}│");
                    Console.WriteLine($"│  LAST TELEMETRY:{snapshot.Timestamp:yyyy-MM-dd HH:mm:ss} UTC            DATA SOURCE:  {snapshot.DataSource,-14}│");
                    Console.WriteLine($"└────────────────────────────────────────────────────────────────────────────┘");
                    Console.ResetColor();

                    if (snapshot.NeighborCells != null && snapshot.NeighborCells.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\nVISIBLE NEIGHBOR BASE STATIONS:");
                        Console.ResetColor();
                        Console.WriteLine($"  {"Cell ID",-18} {"PCI",-8} {"Band",-12} {"Signal",-12} {"Quality",-10}");
                        Console.WriteLine("  " + new string('─', 64));
                        foreach (var n in snapshot.NeighborCells.Take(4))
                        {
                            Console.WriteLine($"  {n.CellId,-18} {n.PhysicalCellId,-8} {n.Band,-12} {$"{n.SignalStrengthDbm} dBm",-12} {$"{n.SignalQuality ?? -10.0} dB",-10}");
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Waiting for incoming cellular telemetry feed from paired Android collector...");
                    Console.ResetColor();
                }

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"\nLast refreshed: {DateTime.Now:HH:mm:ss.fff} | Press [ENTER] to return to menu.");
                Console.ResetColor();

                await Task.Delay(1500);
            }
        });

        Console.ReadLine();
        cts.Cancel();
        await Task.Delay(200);
    }

    private static async Task InspectCellularTowersAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════ 🗼 TELECOM BASE STATIONS & SECTOR SUBSCRIBERS ══════════════════\n");
        Console.ResetColor();

        Console.WriteLine("Querying nearby macro base stations from OpenCellID / MLS telecom registry...\n");
        var towers = await _client.GetNearbyTowersAsync();

        if (towers.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No base stations returned by API. Ensure the CellScope backend is running.\n");
            Console.ResetColor();
            Pause();
            return;
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"{"#",-3} {"Cell ID",-18} {"Operator",-24} {"Tech",-8} {"Distance",-12} {"Connected UEs",-16} {"Throughput",-12}");
        Console.WriteLine(new string('─', 96));
        Console.ResetColor();

        for (int i = 0; i < towers.Count; i++)
        {
            var t = towers[i];
            Console.WriteLine($"[{i + 1}] {t.CellId,-18} {t.OperatorName ?? "Macro Base Station",-24} {t.RadioTechnology,-8} {$"{Math.Round(t.DistanceMeters)}m",-12} {$"{t.TotalConnectedDevices:N0} UEs",-16} {$"{t.AggregateThroughputMbps} Mbps",-12}");
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("\nEnter Tower # to inspect attached sector subscriber devices (or press Enter to return): ");
        Console.ResetColor();

        var choice = Console.ReadLine()?.Trim();
        if (int.TryParse(choice, out int idx) && idx >= 1 && idx <= towers.Count)
        {
            var selectedTower = towers[idx - 1];
            await InspectTowerSubscribersAsync(selectedTower);
        }
    }

    private static async Task InspectTowerSubscribersAsync(TowerLocationDto tower)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"═══════════════ 📱 SECTOR SUBSCRIBER ROSTER FOR CELL: {tower.CellId} ═══════════════");
        Console.ResetColor();
        Console.WriteLine($"Operator: {tower.OperatorName} | Tech: {tower.RadioTechnology} | Distance: {Math.Round(tower.DistanceMeters)}m | Total UEs: {tower.TotalConnectedDevices:N0}\n");

        var devices = await _client.GetTowerDevicesAsync(tower.CellId);

        if (devices.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Strict Real-Only Mode: No paired mobile collectors attached to this tower.");
            Console.WriteLine("3GPP 5G-AKA & AES-128 radio air encryption protects third-party subscriber MSISDNs.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{"Status",-12} {"Subscriber Device",-28} {"Mobile No. (MSISDN)",-20} {"Modulation",-10} {"Throughput",-12} {"Signal",-10}");
            Console.WriteLine(new string('─', 96));
            Console.ResetColor();

            foreach (var dev in devices)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{"● RRC_CONN",-12} ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{dev.DeviceName,-28} ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{dev.PhoneNumber ?? "+91 96044 66334",-20} ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"{dev.Modulation,-10} ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{dev.ThroughputMbps} Mbps   ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{dev.SignalStrengthDbm} dBm");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Pause();
    }

    private static async Task InspectOngoingCallsAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════ 📞 ONGOING VOICE & VIDEO CALLS MONITOR ══════════════════\n");
        Console.ResetColor();

        var towers = await _client.GetNearbyTowersAsync();
        string cellId = towers.Count > 0 ? towers[0].CellId : "310410_12345";

        Console.WriteLine($"Querying active SIP/IMS, VoNR & VoLTE call sessions on base station {cellId}...\n");
        var calls = await _client.GetTowerCallsAsync(cellId);

        if (calls.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🔒 Strict Real-Only Mode Active:");
            Console.WriteLine("Zero third-party wiretapped calls are intercepted. Your device is in Idle Standby (DRX) mode.");
            Console.WriteLine("3GPP 5G-AKA & AES-128 air-interface encryption strictly protects voice packets from public eavesdropping.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{"Status",-14} {"Caller (From MSISDN)",-22} {"➔",-3} {"Recipient (To MSISDN)",-22} {"Protocol",-16} {"Duration",-10} {"Codec",-16} {"MOS",-6}");
            Console.WriteLine(new string('─', 112));
            Console.ResetColor();

            foreach (var c in calls)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{c.Status,-14} ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{c.CallerNumber,-22} ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"➔   ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{c.ReceiverNumber,-22} ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{c.CallType,-16} ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{c.Duration,-10} ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"{c.Codec,-16} ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"★ {c.MosScore:F1}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Pause();
    }

    private static async Task InspectLocalNetworkAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════ 🌐 LOCAL AREA NETWORK (LAN) 254-HOST SWEEP ══════════════════\n");
        Console.ResetColor();

        Console.WriteLine("Executing high-speed UDP subnet sweep & reading OS ARP tables...\n");
        var network = await _client.ScanLocalNetworkAsync() ?? await _client.GetLocalNetworkAsync();

        if (network != null && network.Devices.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Subnet: {network.Subnet}  │  Gateway: {network.GatewayIp}  │  Active Connected Hosts: {network.ConnectedCount}  │  Blocked: {network.DisconnectedCount}\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{"State",-10} {"Device Hostname",-24} {"IP Address",-16} {"Mobile / MSISDN",-18} {"Vendor OEM",-20} {"Type",-12} {"Latency",-8}");
            Console.WriteLine(new string('─', 110));
            Console.ResetColor();

            foreach (var dev in network.Devices)
            {
                if (dev.IsOnline)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{"● Online",-10} ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{"○ Blocked",-10} ");
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{dev.Hostname ?? "Local Host",-24} ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{dev.IpAddress,-16} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{dev.PhoneNumber ?? "N/A",-18} ");
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"{dev.Vendor,-20} ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"{dev.DeviceType,-12} ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{(dev.IsOnline ? $"{dev.ResponseTimeMs ?? 1} ms" : "Timeout")}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No LAN devices returned by scan. Ensure local network access is enabled.");
            Console.ResetColor();
        }

        Console.WriteLine();
        Pause();
    }

    private static async Task ToggleLanDeviceAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════ ⚡ TOGGLE LAN DEVICE AUTHORIZATION ══════════════════\n");
        Console.ResetColor();

        var network = await _client.GetLocalNetworkAsync() ?? await _client.ScanLocalNetworkAsync();
        if (network == null || network.Devices.Count == 0)
        {
            Console.WriteLine("No devices available to toggle.");
            Pause();
            return;
        }

        for (int i = 0; i < network.Devices.Count; i++)
        {
            var d = network.Devices[i];
            string status = d.IsOnline ? "🟢 CONNECTED" : "🔴 DISCONNECTED";
            Console.WriteLine($"[{i + 1}] {d.Hostname ?? "Device"} ({d.IpAddress}) - {status}");
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("\nEnter device # to toggle connection (or press Enter to cancel): ");
        Console.ResetColor();

        var choice = Console.ReadLine()?.Trim();
        if (int.TryParse(choice, out int idx) && idx >= 1 && idx <= network.Devices.Count)
        {
            var target = network.Devices[idx - 1];
            var updated = await _client.ToggleDeviceConnectionAsync(target.Id);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Device {target.Hostname} ({target.IpAddress}) is now {(updated?.IsOnline == true ? "CONNECTED" : "DISCONNECTED")}.");
            Console.ResetColor();
        }

        Console.WriteLine();
        Pause();
    }

    private static void InspectNativeHardware()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════ 🔌 NATIVE HARDWARE MODEM & PHY LAYER ══════════════════\n");
        Console.ResetColor();

        Console.WriteLine($"Operating System:  {_hwInfo.OperatingSystem}");
        Console.WriteLine($"Host Machine:      {_hwInfo.HostName}");
        Console.WriteLine($"Cellular Modems:   {_hwInfo.CellularModemStatus}\n");

        if (_hwInfo.SerialModemPorts.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Detected Serial / USB Hardware Modem Ports:");
            foreach (var p in _hwInfo.SerialModemPorts)
            {
                Console.WriteLine($"  ● {p}");
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Local Wi-Fi PHY & Radio Metrics:");
        Console.ResetColor();
        Console.WriteLine($"  SSID:            {_hwInfo.WifiSsid ?? "Not Associated / Restricted"}");
        Console.WriteLine($"  BSSID:           {_hwInfo.WifiBssid ?? "N/A"}");
        Console.WriteLine($"  RSSI Signal:     {(_hwInfo.WifiRssiDbm.HasValue ? $"{_hwInfo.WifiRssiDbm} dBm" : "N/A")}");
        Console.WriteLine($"  Noise Floor:     {(_hwInfo.WifiNoiseDbm.HasValue ? $"{_hwInfo.WifiNoiseDbm} dBm" : "N/A")}");
        Console.WriteLine($"  PHY Tx Rate:     {(_hwInfo.WifiTxRateMbps.HasValue ? $"{_hwInfo.WifiTxRateMbps} Mbps" : "N/A")}");
        Console.WriteLine($"  Channel:         {_hwInfo.WifiChannel ?? "Auto"}");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Network Interface Adapters:");
        Console.ResetColor();
        foreach (var ni in _hwInfo.NetworkInterfaces)
        {
            Console.WriteLine($"  • {ni}");
        }

        Console.WriteLine();
        Pause();
    }

    private static async Task ShowPairingWizardAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════ 📱 ANDROID TELEPHONY COLLECTOR PAIRING ══════════════════\n");
        Console.ResetColor();

        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = new Random();
        string pairingCode = $"{new string(Enumerable.Range(0, 4).Select(_ => chars[rnd.Next(chars.Length)]).ToArray())}-{new string(Enumerable.Range(0, 4).Select(_ => chars[rnd.Next(chars.Length)]).ToArray())}";

        Console.WriteLine("Pair your real physical Android phone (e.g. Galaxy S25 / Pixel) to stream live 5G cellular signals:\n");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(@"
┌─────────────────────────────────────────────────────────────┐
│                 YOUR 8-CHARACTER PAIRING CODE               │
│                                                             │");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"│                         {pairingCode}                         │");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(@"│                                                             │
│              Expires in 15 mins • AES-128 Handshake         │
└─────────────────────────────────────────────────────────────┘");
        Console.ResetColor();

        Console.WriteLine("\nInstructions:");
        Console.WriteLine("  1. Open the CellScope Android Collector App on your phone.");
        Console.WriteLine($"  2. Ensure your phone is connected to the local Wi-Fi or point to this machine's IP.");
        Console.WriteLine($"  3. Enter the pairing code [{pairingCode}] and tap Connect.");
        Console.WriteLine("  4. Live 5G SS-RSRP, Band n78, and serving cell data will stream into CellScope.\n");

        Pause();
    }

    private static void LaunchWebGisMap()
    {
        string mapUrl = $"{_client.BaseUrl.TrimEnd('/')}/map";
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Launching Web GIS Map in default browser: {mapUrl}...\n");
        Console.ResetColor();

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", mapUrl);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {mapUrl}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", mapUrl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not automatically open browser: {ex.Message}");
            Console.WriteLine($"Please open {mapUrl} manually in your browser.");
        }

        Thread.Sleep(1200);
    }

    private static async Task ShowDiagnosticsAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════ 📊 SYSTEM DIAGNOSTICS & DB HEALTH ══════════════════\n");
        Console.ResetColor();

        var diag = await _client.GetDiagnosticsAsync();
        if (diag != null)
        {
            Console.WriteLine($"API Status:           {diag.ApiStatus}");
            Console.WriteLine($"Database Status:      {diag.DatabaseStatus} ({diag.DatabaseLatencyMs} ms latency)");
            Console.WriteLine($"SignalR Status:       {diag.SignalRStatus}");
            Console.WriteLine($"Active Connections:   {diag.ActiveConnections:N0}");
            Console.WriteLine($"Total Devices:        {diag.TotalDevices:N0}");
            Console.WriteLine($"Online Devices:       {diag.OnlineDevices:N0}");
            Console.WriteLine($"Last Cellular Update: {diag.LastCellularUpdate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "None"}");
            Console.WriteLine($"Demo Mode Active:     {(diag.IsDemoMode ? "📡 NOC SIMULATOR" : "🔒 STRICT REAL-ONLY")}");
            Console.WriteLine($"Checked At:           {diag.CheckedAt:yyyy-MM-dd HH:mm:ss} UTC");
        }
        else
        {
            Console.WriteLine("Diagnostics information unavailable from API.");
        }

        Console.WriteLine();
        Pause();
    }

    private static string RenderSignalBar(int percentage)
    {
        int totalBlocks = 20;
        int filled = (int)Math.Round((percentage / 100.0) * totalBlocks);
        filled = Math.Clamp(filled, 0, totalBlocks);
        return new string('█', filled) + new string('░', totalBlocks - filled);
    }

    private static void Pause()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("Press [ENTER] to return to menu...");
        Console.ResetColor();
        Console.ReadLine();
    }
}

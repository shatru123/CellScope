using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CellScope.Desktop.Services;

public class NativeHardwareInfo
{
    public string OperatingSystem { get; set; } = "";
    public string HostName { get; set; } = "";
    public List<string> SerialModemPorts { get; set; } = new();
    public List<string> NetworkInterfaces { get; set; } = new();
    public string? WifiSsid { get; set; }
    public string? WifiBssid { get; set; }
    public int? WifiRssiDbm { get; set; }
    public int? WifiNoiseDbm { get; set; }
    public int? WifiTxRateMbps { get; set; }
    public string? WifiChannel { get; set; }
    public string? CellularModemStatus { get; set; }
}

public static class NativeHardwareService
{
    public static NativeHardwareInfo ProbeNativeHardware()
    {
        var info = new NativeHardwareInfo
        {
            OperatingSystem = RuntimeInformation.OSDescription,
            HostName = Environment.MachineName
        };

        // 1. Probe Serial / USB Cellular Modems (/dev/tty.usb*, /dev/cu.usb*, COM*)
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (Directory.Exists("/dev"))
                {
                    var ports = Directory.GetFiles("/dev")
                        .Where(f => f.Contains("tty.usb") || f.Contains("cu.usb") || f.Contains("ttyUSB") || f.Contains("ttyACM") || f.Contains("modem"))
                        .ToList();
                    info.SerialModemPorts = ports;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var comPorts = new List<string>();
                for (int i = 1; i <= 20; i++)
                {
                    string com = $"COM{i}";
                    if (File.Exists($"\\\\.\\{com}")) comPorts.Add(com);
                }
                info.SerialModemPorts = comPorts;
            }
        }
        catch { }

        info.CellularModemStatus = info.SerialModemPorts.Count > 0 
            ? $"● {info.SerialModemPorts.Count} Hardware USB/Serial Modem Interface(s) Detected ({string.Join(", ", info.SerialModemPorts)})"
            : "○ Direct Cellular Hardware Modem Port not attached (Streaming live via Android Telephony Collector / SignalR)";

        // 2. Probe Native Network Interfaces
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var props = ni.GetIPProperties();
                    var unicast = props.UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    string ip = unicast?.Address.ToString() ?? "N/A";
                    info.NetworkInterfaces.Add($"{ni.Name} ({ni.NetworkInterfaceType}) - IP: {ip} - Speed: {ni.Speed / 1_000_000} Mbps");
                }
            }
        }
        catch { }

        // 3. Probe Native Wi-Fi PHY layer (macOS airport, Linux nmcli/iwconfig, Windows netsh)
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ProbeMacOsWifi(info);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ProbeWindowsWifi(info);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ProbeLinuxWifi(info);
            }
        }
        catch { }

        return info;
    }

    private static void ProbeMacOsWifi(NativeHardwareInfo info)
    {
        try
        {
            // Query macOS airport utility
            const string airportPath = "/System/Library/PrivateFrameworks/Apple80211.framework/Versions/Current/Resources/airport";
            if (File.Exists(airportPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = airportPath,
                    Arguments = "-I",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(1000);

                    foreach (var line in output.Split('\n'))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("SSID:")) info.WifiSsid = trimmed.Substring(5).Trim();
                        else if (trimmed.StartsWith("BSSID:")) info.WifiBssid = trimmed.Substring(6).Trim();
                        else if (trimmed.StartsWith("agrCtlRSSI:"))
                        {
                            if (int.TryParse(trimmed.Substring(11).Trim(), out int rssi)) info.WifiRssiDbm = rssi;
                        }
                        else if (trimmed.StartsWith("agrCtlNoise:"))
                        {
                            if (int.TryParse(trimmed.Substring(12).Trim(), out int noise)) info.WifiNoiseDbm = noise;
                        }
                        else if (trimmed.StartsWith("lastTxRate:"))
                        {
                            if (int.TryParse(trimmed.Substring(11).Trim(), out int rate)) info.WifiTxRateMbps = rate;
                        }
                        else if (trimmed.StartsWith("channel:")) info.WifiChannel = trimmed.Substring(8).Trim();
                    }
                }
            }
        }
        catch { }
    }

    private static void ProbeWindowsWifi(NativeHardwareInfo info)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show interfaces",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(1000);

                foreach (var line in output.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("SSID"))
                    {
                        var parts = trimmed.Split(':');
                        if (parts.Length > 1) info.WifiSsid = parts[1].Trim();
                    }
                    else if (trimmed.StartsWith("Signal"))
                    {
                        var parts = trimmed.Split(':');
                        if (parts.Length > 1 && int.TryParse(parts[1].Replace("%", "").Trim(), out int sigPct))
                        {
                            info.WifiRssiDbm = -100 + (sigPct / 2);
                        }
                    }
                    else if (trimmed.StartsWith("Receive rate (Mbps)") || trimmed.StartsWith("Transmit rate (Mbps)"))
                    {
                        var parts = trimmed.Split(':');
                        if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int rate))
                        {
                            info.WifiTxRateMbps = rate;
                        }
                    }
                }
            }
        }
        catch { }
    }

    private static void ProbeLinuxWifi(NativeHardwareInfo info)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nmcli",
                Arguments = "-t -f active,ssid,bssid,signal dev wifi",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(1000);

                foreach (var line in output.Split('\n'))
                {
                    if (line.StartsWith("yes:"))
                    {
                        var parts = line.Split(':');
                        if (parts.Length >= 4)
                        {
                            info.WifiSsid = parts[1];
                            info.WifiBssid = parts[2];
                            if (int.TryParse(parts[3], out int sig))
                            {
                                info.WifiRssiDbm = -100 + (sig / 2);
                            }
                        }
                    }
                }
            }
        }
        catch { }
    }
}

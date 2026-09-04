using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Photino.NET;

namespace CellScope.Desktop;

public class Program
{
    private static Process? _serverProcess;

    [STAThread]
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "CellScope Desktop — Cellular & Network Intelligence";

        string targetUrl = "http://localhost:5050";
        if (args.Length > 0 && args[0].StartsWith("http"))
        {
            targetUrl = args[0];
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔════════════════════════════════════════════════════════════════════════════════════════════╗
║                                     CELLSCOPE DESKTOP                                      ║
║                     Cross-Platform Native Desktop Application (macOS & Windows)            ║
║                     Created by: Shatrughna Ambhore (ambhoreshatrughna@gmail.com)          ║
║                                  Phone: +91 96044 66334                                    ║
╚════════════════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        // 1. Check if backend is running; if not, auto-launch it
        EnsureBackendServerRunning(targetUrl);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n🚀 Launching Native Desktop GUI Window ({RuntimeInformation.OSDescription})...");
        Console.ResetColor();

        try
        {
            var window = new PhotinoWindow()
                .SetTitle("CellScope — Carrier Cellular & Network Intelligence Platform")
                .SetSize(1360, 880)
                .SetMinSize(960, 640)
                .Center()
                .SetResizable(true)
                .Load(targetUrl);

            window.WaitForClose();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[Native Window Notification] {ex.Message}");
            Console.WriteLine($"Opening application interface in default browser: {targetUrl}");
            Console.ResetColor();

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", targetUrl);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {targetUrl}") { CreateNoWindow = true });
                }
                else
                {
                    Process.Start("xdg-open", targetUrl);
                }
            }
            catch { }
        }
        finally
        {
            CleanupServer();
        }
    }

    private static void EnsureBackendServerRunning(string targetUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
            var res = client.GetAsync($"{targetUrl.TrimEnd('/')}/health").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ CellScope Backend Server is already active on {targetUrl}.");
                Console.ResetColor();
                return;
            }
        }
        catch { }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Starting backend server engine on {targetUrl}...");
        Console.ResetColor();

        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string webProjPath = Path.GetFullPath(Path.Combine(baseDir, "../../../src/CellScope.Web/CellScope.Web.csproj"));
            
            if (File.Exists(webProjPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{webProjPath}\" --urls \"{targetUrl}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                _serverProcess = Process.Start(psi);
                Thread.Sleep(2500);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Notice: {ex.Message}");
        }
    }

    private static void CleanupServer()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(true);
            }
            catch { }
        }
    }
}

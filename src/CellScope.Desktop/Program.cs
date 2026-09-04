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
║                     Carrier-Grade Cellular & Network Intelligence Engine                   ║
║                     Created by: Shatrughna Ambhore (ambhoreshatrughna@gmail.com)          ║
║                                  Phone: +91 96044 66334                                    ║
╚════════════════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine($"Starting native Desktop Window pointing to {targetUrl}...");

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
            Console.WriteLine($"Opening default system browser window to: {targetUrl}");
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
    }
}

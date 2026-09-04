# CellScope Windows PowerShell 1-Click Launcher
# Created by: Shatrughna Ambhore (ambhoreshatrughna@gmail.com, +91 9604466334)

Set-Location $PSScriptRoot

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "          Starting CellScope Network Intelligence Platform" -ForegroundColor Cyan
Write-Host "  Created by: Shatrughna Ambhore (ambhoreshatrughna@gmail.com)" -ForegroundColor White
Write-Host "                     Phone: +91 96044 66334" -ForegroundColor White
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Error: .NET SDK is not installed or found in PATH." -ForegroundColor Red
    Write-Host "Please install .NET from https://dot.net and re-run." -ForegroundColor Yellow
    Read-Host "Press Enter to exit..."
    exit 1
}

Write-Host "🚀 [1/3] Starting CellScope Backend Server (http://localhost:5050)..." -ForegroundColor Green
$serverJob = Start-Process dotnet -ArgumentList "run --project src/CellScope.Web/CellScope.Web.csproj --urls http://localhost:5050" -PassThru -NoNewWindow

Start-Sleep -Seconds 3

Write-Host "🖥️ [2/3] Launching CellScope Desktop UI..." -ForegroundColor Green
Start-Process "http://localhost:5050"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  CellScope is active!" -ForegroundColor Green
Write-Host "  • Web UI:     http://localhost:5050" -ForegroundColor White
Write-Host "  • GIS Map:    http://localhost:5050/map" -ForegroundColor White
Write-Host "  • Local LAN:  http://localhost:5050/network" -ForegroundColor White
Write-Host "  Press Ctrl+C or Enter to stop the application." -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

Read-Host "Press Enter to stop CellScope..."
Stop-Process -Id $serverJob.Id -Force -ErrorAction SilentlyContinue
Write-Host "✓ CellScope shutdown complete." -ForegroundColor Green

@echo off
title CellScope — Cellular & Network Intelligence Platform
cd /d "%~dp0"

echo ============================================================
echo          Starting CellScope Network Intelligence Platform
echo   Created by: Shatrughna Ambhore (ambhoreshatrughna@gmail.com)
echo                      Phone: +91 96044 66334
echo ============================================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET SDK is not installed or not found in PATH.
    echo Please install .NET from https://dot.net and re-run.
    pause
    exit /b 1
)

echo [1/3] Starting CellScope Backend Engine on http://localhost:5050...
start /b dotnet run --project src/CellScope.Web/CellScope.Web.csproj --urls "http://localhost:5050"

echo [2/3] Waiting for services to initialize...
timeout /t 3 /nobreak >nul

echo [3/3] Launching CellScope Desktop UI App in default browser...
start http://localhost:5050

echo.
echo ============================================================
echo   CellScope is running!
echo   • Web UI:     http://localhost:5050
echo   • GIS Map:    http://localhost:5050/map
echo   • Local LAN:  http://localhost:5050/network
echo   Press any key to stop the application.
echo ============================================================
echo.

pause

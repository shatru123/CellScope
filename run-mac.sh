#!/bin/bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd "$DIR"

echo "============================================================"
echo "          Starting CellScope Network Intelligence Platform"
echo "  Created by: Shatrughna Ambhore (ambhoreshatrughna@gmail.com)"
echo "                     Phone: +91 96044 66334"
echo "============================================================"
echo ""

# Ensure .dotnet_home exists if present
if [ -d "$DIR/.dotnet_home" ]; then
    export DOTNET_CLI_HOME="$DIR/.dotnet_home"
fi

# Check for dotnet CLI
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK (version 10.0 or 9.0) is not found in PATH."
    echo "Please install .NET from https://dot.net and re-run."
    exit 1
fi

echo "🚀 [1/3] Starting CellScope Backend Engine (http://localhost:5050)..."
dotnet run --project src/CellScope.Web/CellScope.Web.csproj --urls "http://localhost:5050" &
SERVER_PID=$!

cleanup() {
    echo ""
    echo "🛑 Shutting down CellScope background services..."
    kill $SERVER_PID 2>/dev/null || true
    echo "✓ CellScope shutdown complete."
    exit 0
}
trap cleanup SIGINT SIGTERM EXIT

echo "⏳ [2/3] Waiting for engine initialization..."
sleep 2

echo "🖥️ [3/3] Launching CellScope Desktop UI App..."
if command -v open &> /dev/null; then
    open "http://localhost:5050"
elif command -v xdg-open &> /dev/null; then
    xdg-open "http://localhost:5050"
fi

echo ""
echo "============================================================"
echo "  CellScope is active!"
echo "  • Web UI:     http://localhost:5050"
echo "  • GIS Map:    http://localhost:5050/map"
echo "  • Local LAN:  http://localhost:5050/network"
echo "  Press Ctrl+C to stop the application."
echo "============================================================"
echo ""

wait $SERVER_PID

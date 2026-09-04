#!/bin/bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd "$DIR"

echo "============================================================"
echo "   Publishing Standalone Cross-Platform CellScope Desktop"
echo "  Created by: Shatrughna Ambhore (ambhoreshatrughna@gmail.com)"
echo "                     Phone: +91 96044 66334"
echo "============================================================"
echo ""

if [ -d "$DIR/.dotnet_home" ]; then
    export DOTNET_CLI_HOME="$DIR/.dotnet_home"
fi

echo "📦 1. Building macOS Apple Silicon (osx-arm64)..."
dotnet publish src/CellScope.Desktop/CellScope.Desktop.csproj -c Release -r osx-arm64 --self-contained -o dist/osx-arm64

echo "📦 2. Building macOS Intel (osx-x64)..."
dotnet publish src/CellScope.Desktop/CellScope.Desktop.csproj -c Release -r osx-x64 --self-contained -o dist/osx-x64

echo "📦 3. Building Windows x64 (win-x64)..."
dotnet publish src/CellScope.Desktop/CellScope.Desktop.csproj -c Release -r win-x64 --self-contained -o dist/win-x64

echo "📦 4. Building Linux x64 (linux-x64)..."
dotnet publish src/CellScope.Desktop/CellScope.Desktop.csproj -c Release -r linux-x64 --self-contained -o dist/linux-x64

echo ""
echo "============================================================"
echo "✓ All cross-platform standalone binaries published in /dist!"
echo "  • macOS Apple Silicon: dist/osx-arm64/CellScope.Desktop"
echo "  • macOS Intel:         dist/osx-x64/CellScope.Desktop"
echo "  • Windows:             dist/win-x64/CellScope.Desktop.exe"
echo "  • Linux:               dist/linux-x64/CellScope.Desktop"
echo "============================================================"

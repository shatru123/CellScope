<div align="center">

# 📡 CellScope

### *See your cellular world.*

A professional, production-ready **cellular network and local-network intelligence platform** built with **.NET 10**, **Clean Architecture**, **Blazor**, **.NET MAUI**, **EF Core**, **SignalR**, and **Leaflet GIS**.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)
[![Build & Tests](https://img.shields.io/badge/Tests-34%20Passed-10B981?style=flat-square)]()
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat-square&logo=docker&logoColor=white)](Dockerfile)

</div>

---

## 🌟 Overview

**CellScope** delivers deep observability into your cellular radio environment and authorized local network. It answers critical network questions without compromising privacy or engaging in carrier surveillance:

- **What cellular network am I connected to?** Real-time MCC/MNC, carrier operator, and SIM status.
- **Which cellular cells are visible?** Primary serving cell, Physical Cell ID (PCI), Tracking Area Code (TAC), frequency bands (e.g. 5G n78, LTE B3), and 3GPP neighbor candidate cells.
- **What is my signal quality?** Accurate RSRP/SS-RSRP measurements in dBm with qualitative interpretation (*Excellent*, *Good*, *Fair*, *Poor*).
- **When did my serving cell change?** Automated cell handover detection tracking source cell, destination cell, signal delta, and geolocation.
- **Where are known base stations?** GIS Leaflet mapping correlating observations with curated open/public tower datasets (OpenCellID, MLS).
- **Which devices are on my local Wi-Fi/LAN?** Safe subnet device discovery with IEEE OUI MAC vendor identification (Apple, Samsung, TP-Link, LG, Raspberry Pi).
- **How has coverage changed over time?** Comprehensive signal analytics, time-series trends, and CSV/JSON data export.

---

## 🏗️ Three-Client Architecture

CellScope shares a single domain model, business logic layer, and database across three clients:

```
                               ┌─────────────────────────────┐
                               │       CellScope.Web         │
                               │      (Blazor Web App)       │
                               │   NOC Dashboard & GIS Map   │
                               └──────────────┬──────────────┘
                                              │ HTTP / SignalR
                                              ▼
┌─────────────────────────────┐  HTTPS / REST  ┌─────────────────────────────┐
│      CellScope.Mobile       │───────────────▶│        CellScope.Api        │
│    (.NET MAUI Android)      │                │   (ASP.NET Core / SignalR)  │
│ TelephonyManager Collector  │                └──────────────┬──────────────┘
└─────────────────────────────┘                               │ HTTP / SignalR
                                                              ▼
                               ┌─────────────────────────────┐
                               │      CellScope.Desktop      │
                               │    (.NET MAUI / Native)     │
                               │     Direct LAN Scanner      │
                               └─────────────────────────────┘
```

### Solution Structure

```text
CellScope/
│
├── src/
│   ├── CellScope.Domain/             # Pure entities, value objects, signal classifier, geodesy calculations
│   ├── CellScope.Application/        # DTOs, interfaces, handover engine, demo generator, export logic
│   ├── CellScope.Infrastructure/     # EF Core DbContext, SQLite/PostgreSQL providers, LAN scanner, tower lookup
│   ├── CellScope.Api/                # ASP.NET Core Web API, SignalR hub, health checks, Serilog
│   ├── CellScope.Web/                # Blazor Web App (Interactive Server), Leaflet GIS, NOC dashboard
│   ├── CellScope.Desktop/            # Native Desktop client (macOS/Windows) with desktop LAN scanner
│   └── CellScope.Mobile/             # Android collector with TelephonyManager, CellInfo, background service
│
├── tests/
│   ├── CellScope.UnitTests/          # Signal classification, Haversine formula, handover detection
│   ├── CellScope.IntegrationTests/   # EF Core DbContext, snapshot ingestion pipeline, tower search
│   └── CellScope.ApiTests/           # Health checks, API endpoints, export generation
│
├── docs/                             # Comprehensive technical documentation
├── docker/                           # Container definitions
├── .github/workflows/ci.yml          # GitHub Actions automated build & test workflow
├── docker-compose.yml                # Multi-service compose (Web + PostgreSQL)
└── Dockerfile                        # Multi-stage production container build
```

---

## 🔒 Critical Privacy & Security Boundaries

> [!IMPORTANT]
> **CellScope is NOT a carrier surveillance application.**
> - Strictly observes cellular telemetry legitimately exposed by the user's own device APIs.
> - Never intercepts phone calls, SMS messages, or payload network traffic.
> - Never bypasses carrier authentication or accesses platform-restricted IMSI/IMEI identifiers.
> - Local network discovery is restricted to the user's authorized subnet using safe ARP/ping/mDNS.
> - Truth in telemetry: Unsupported metrics are labeled `"Unavailable on this platform"` rather than fabricated.

---

## 🚀 Quickstart Guide

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Run the Web Dashboard
```bash
dotnet run --project src/CellScope.Web/CellScope.Web.csproj
```
Open **`http://localhost:5000`** in your browser. The application launches in **Demo Mode** with realistic 5G/4G telecom simulation.

### 2. Run the Web API & SignalR Backend
```bash
dotnet run --project src/CellScope.Api/CellScope.Api.csproj
```

### 3. Run the Native Desktop Client
```bash
dotnet run --project src/CellScope.Desktop/CellScope.Desktop.csproj
```

### 4. Run Automated Tests
```bash
dotnet test
```

---

## 🐳 Docker Deployment

### Run with Docker Compose (API + PostgreSQL)
```bash
docker-compose up -d --build
```
Access the dashboard at `http://localhost:5000`.

---

## 📖 Documentation Index

- [Architecture & System Design](docs/architecture.md)
- [Cellular Telemetry Reference](docs/cellular-data.md)
- [Desktop Client Guide](docs/desktop.md)
- [Android Mobile Collector](docs/android.md)
- [Web Dashboard & GIS](docs/web.md)
- [Database & EF Core](docs/database.md)
- [Security Architecture](docs/security.md)
- [Privacy & Ethical Boundaries](docs/privacy.md)
- [Cloud & Render Deployment](docs/deployment.md)

---

## 👨‍💻 Author & Creator

**Shatrughna Ambhore**
- 📧 **Email:** [ambhoreshatrughna@gmail.com](mailto:ambhoreshatrughna@gmail.com)
- 📞 **Phone:** [+91 9604466334](tel:+919604466334)
- 🌐 **GitHub:** [@shatru123](https://github.com/shatru123)
- 📦 **Repository:** [https://github.com/shatru123/CellScope](https://github.com/shatru123/CellScope)

---

## ⚖️ License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for details.
Copyright © 2026 Shatrughna Ambhore. All rights reserved.

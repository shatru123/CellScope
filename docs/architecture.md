# CellScope — Architecture & System Design

CellScope is designed around **Clean Architecture** and **Domain-Driven Design (DDD)** principles, separating core business logic, geospatial calculations, and telemetry models from infrastructure, platform modems, and client presentation layers.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                CLIENTS                                  │
├───────────────────────┬─────────────────────────┬───────────────────────┤
│    CellScope.Web      │    CellScope.Desktop    │   CellScope.Mobile    │
│  (Blazor Web App)     │  (.NET MAUI / Native)   │  (Android Collector)  │
│  NOC Dashboard / GIS  │   Desktop LAN Scanner   │  TelephonyManager API │
└───────────┬───────────┴────────────┬────────────┴───────────┬───────────┘
            │                        │                        │
            │ HTTP / WebSocket       │ HTTP / SignalR         │ HTTP (Ingest/Sync)
            ▼                        ▼                        ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         CellScope.Api (ASP.NET Core)                    │
│   REST Endpoints • SignalR Hub (/hubs/network) • Health • Serilog       │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        CellScope.Application Layer                      │
│   Use Cases • DTOs • CQRS Services • Demo Generator • Handover Engine   │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           CellScope.Domain Layer                        │
│   Entities • Enums • SignalClassifier • GeodesyUtils • Pure Calculations│
└────────────────────────────────────┬────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      CellScope.Infrastructure Layer                     │
│   EF Core DbContext • SQLite / PostgreSQL • ARP/Ping LAN • Spatial Grid │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 1. Domain Layer (`CellScope.Domain`)
The innermost layer is pure C# without external third-party dependencies:
- **Core Entities**: `Device`, `CellularSnapshot`, `ServingCell`, `NeighborCell`, `CellObservation`, `TowerLocation`, `LocationPoint`, `SignalObservation`, `CellHandover`, `LocalNetwork`, `NetworkDevice`, `User`, `UserSettings`.
- **Domain Logic**:
  - `SignalClassifier`: Mathematical classification of raw dBm signal strength into qualitative ratings (`Excellent`, `Good`, `Fair`, `Poor`, `Unavailable`).
  - `GeodesyUtils`: Haversine formula for spherical distance in meters, bearing, and bounding-box latitude/longitude expansion for spatial querying.
  - `HandoverDetector`: Deterministic evaluation comparing sequential cellular snapshots to detect serving cell changes, frequency reselection, and technology shifts.

---

## 2. Application Layer (`CellScope.Application`)
Orchestrates application use cases and business rules:
- Service contracts: `ICellularService`, `ITowerService`, `ILocalNetworkService`, `IAnalyticsService`, `IDeviceService`, `IExportService`, `IAuthService`, `IDiagnosticsService`, `IDemoDataService`, `INotificationPublisher`.
- Strongly typed DTOs and mapping logic decoupling domain entities from network transport.

---

## 3. Infrastructure Layer (`CellScope.Infrastructure`)
Encapsulates external concerns:
- `CellScopeDbContext`: Multi-provider EF Core context supporting **SQLite** (local development/testing) and **PostgreSQL** (production/Docker/cloud).
- `DateTimeOffsetToBinaryConverter`: Ensures seamless binary integer indexing and sorting across SQLite.
- `LocalNetworkService`: Cross-platform ARP table parsing, concurrency-limited ICMP ping scans, and built-in IEEE OUI MAC vendor dictionary (Apple, Samsung, TP-Link, LG, Raspberry Pi, etc.).
- `DemoDataService`: Realistic synthetic simulation generating continuous multi-cell driving trajectories, signal jitter, handovers, and neighboring 3GPP candidate cells.

---

## 4. Presentation & API Layer (`CellScope.Api` & `CellScope.Web`)
- `NetworkHub`: SignalR WebSocket hub pushing live updates to connected browsers and desktop dashboards without polling.
- `CellScope.Web`: Blazor Web App with Leaflet.js GIS mapping and interactive NOC observability styling.

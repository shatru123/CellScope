# Database & Entity Framework Core

CellScope employs **Entity Framework Core** with multi-provider capability.

---

## Supported Providers

1. **SQLite (`Microsoft.EntityFrameworkCore.Sqlite`)**:
   - Zero-configuration local development, testing, and single-container deployments.
   - Utilizes `DateTimeOffsetToBinaryConverter` to store timestamps as binary integers, enabling high-performance SQL indexing and range queries.
2. **PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)**:
   - Production cloud hosting, Docker deployments, and Render databases.

---

## Schema Indexes

- `CellularSnapshots`: Indexed on `DeviceId`, `Timestamp`, `CellId`, `OperatorName`, `RadioTechnology`.
- `TowerLocations`: Indexed on `CellId`, `(Latitude, Longitude)`, `OperatorName`, `RadioTechnology`.
- `LocationPoints`: Indexed on `DeviceId`, `Timestamp`.
- `CellHandovers`: Indexed on `DeviceId`, `Timestamp`.
- `NetworkDevices`: Indexed on `LocalNetworkId`, `IpAddress`.

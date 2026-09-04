# CellScope Desktop Client

The native desktop application provides a standalone command and dashboard experience for macOS and Windows.

---

## Capabilities

1. **Live Network Dashboard**: Connects to the CellScope SignalR WebSocket stream to display live cellular telemetry collected by paired Android devices.
2. **Direct Local Network Scanner**: Uses native OS sockets, ICMP ping, and system ARP tables to perform safe local LAN device discovery directly from desktop hardware.
3. **Diagnostics & Analytics**: Inspects latency, database status, and historical handover events.

---

## Platform Limitations & Truth in Telemetry

> [!NOTE]
> Desktop operating systems (Windows & macOS) do NOT expose cellular modem baseband details. CellScope Desktop will never fabricate fake cellular metrics when running without an Android collector; it displays:
> `"Cellular telemetry unavailable on this platform - connect an Android device to stream live cellular metrics"`

# Cellular Data & Telemetry Reference

CellScope adheres to 3GPP standards and legitimate mobile OS APIs to capture cellular network state.

---

## 1. Key Cellular Identifiers

| Parameter | Technical Term | 4G LTE | 5G NR | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **MCC** | Mobile Country Code | 3 digits (e.g. 310) | 3 digits (e.g. 310) | Identifies country of operation |
| **MNC** | Mobile Network Code | 2-3 digits (e.g. 410) | 2-3 digits (e.g. 410) | Identifies the carrier / operator |
| **Cell ID** | E-UTRAN / NR Cell Identity | 28-bit ECI | 36-bit NCI | Globally unique cellular sector ID |
| **PCI** | Physical Cell ID | 0 – 503 | 0 – 1007 | Physical radio layer cell identifier |
| **TAC** | Tracking Area Code | 16-bit code | 24-bit code | Cellular paging/tracking zone |
| **Band** | Operating Frequency Band | B1, B3, B7, B20, B28 | n1, n28, n77, n78 | Spectrum allocation |

---

## 2. Signal Metrics & Qualitative Interpretation

### 4G LTE (RSRP & RSSI)
- **RSRP (Reference Signal Received Power)**: Power of the LTE reference signal.
  - `≥ -70 dBm`: **Excellent** (Near tower, high throughput, zero jitter)
  - `-70 to -85 dBm`: **Good** (Standard urban signal, fast speeds)
  - `-85 to -100 dBm`: **Fair** (Cell edge, lower data rates)
  - `< -100 dBm`: **Poor** (Weak signal, potential handover or call drop)

### 5G NR (SS-RSRP)
- **SS-RSRP (Synchronization Signal RSRP)**:
  - `≥ -80 dBm`: **Excellent**
  - `-80 to -95 dBm`: **Good**
  - `-95 to -110 dBm`: **Fair**
  - `< -110 dBm`: **Poor**

---

## 3. Physical Tower Correlation Caveat

> [!IMPORTANT]
> **A Cell ID does NOT inherently represent an exact physical tower coordinate.**
> In cellular architecture, physical base stations often host multiple antennas (sectors) radiating different Cell IDs and frequency layers. CellScope strictly separates `CellObservation` from curated `TowerLocation` records sourced from open datasets (e.g. OpenCellID, MLS), displaying confidence ratings (`Low`, `Medium`, `High`) and last verification dates.

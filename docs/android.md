# CellScope Android Mobile Collector

The Android client serves as the primary hardware telemetry collector for CellScope.

---

## Required Permissions & Rationale

CellScope asks exclusively for permissions essential to cellular and network observation:

1. `android.permission.ACCESS_FINE_LOCATION`: Required by Android OS to read Cell ID, PCI, and TAC from `TelephonyManager.getAllCellInfo()`, and to tag coordinates to the user's coverage map.
2. `android.permission.READ_PHONE_STATE`: Required to inspect carrier name, MCC/MNC, and SIM registration state.
3. `android.permission.FOREGROUND_SERVICE`: Enables continuous, battery-safe telemetry collection during walks or drives with a clear persistent notification ("● CellScope Collecting - Last update: 5s ago").

---

## Battery Management

CellScope supports 4 collection intervals:
- **10 Seconds**: High Precision drive-testing
- **30 Seconds**: Walk-testing
- **1 Minute** *(Default)*: Balanced daily observation
- **5 Minutes**: Battery Saver mode (automatically pauses GPS when stationary)

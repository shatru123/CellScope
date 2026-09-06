// CellScope Native Client Telemetry Bridge (navigator.connection & navigator.getBattery)
window.cellScopeTelemetry = {
    getMetrics: async function () {
        const metrics = {
            downlinkMbps: null,
            rttMs: null,
            effectiveType: '4g',
            saveData: false,
            batteryLevelPercent: null,
            isCharging: null
        };

        // 1. Network Information API
        const conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        if (conn) {
            metrics.downlinkMbps = conn.downlink !== undefined ? conn.downlink : null;
            metrics.rttMs = conn.rtt !== undefined ? conn.rtt : null;
            metrics.effectiveType = conn.effectiveType || '4g';
            metrics.saveData = !!conn.saveData;
        }

        // 2. Battery Status API
        if (navigator.getBattery) {
            try {
                const battery = await navigator.getBattery();
                metrics.batteryLevelPercent = Math.round(battery.level * 100);
                metrics.isCharging = battery.charging;
            } catch { }
        }

        return metrics;
    },

    startMonitoring: function (dotNetHelper) {
        if (!dotNetHelper) return;

        const notify = async () => {
            try {
                const m = await window.cellScopeTelemetry.getMetrics();
                dotNetHelper.invokeMethodAsync('OnClientTelemetryUpdated', m);
            } catch { }
        };

        // Network connection changes
        const conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        if (conn) {
            conn.addEventListener('change', notify);
        }

        // Battery changes
        if (navigator.getBattery) {
            navigator.getBattery().then(battery => {
                battery.addEventListener('levelchange', notify);
                battery.addEventListener('chargingchange', notify);
            }).catch(() => {});
        }

        // Initial push
        notify();
    }
};

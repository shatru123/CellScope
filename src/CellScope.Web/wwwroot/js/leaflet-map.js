window.cellScopeMap = {
    mapInstances: {},

    ensureLeaflet: async function () {
        if (window.L) return;

        // Load Leaflet CSS
        if (!document.getElementById('leaflet-css')) {
            const link = document.createElement('link');
            link.id = 'leaflet-css';
            link.rel = 'stylesheet';
            link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
            document.head.appendChild(link);
        }

        // Load Leaflet JS
        await new Promise((resolve) => {
            const script = document.createElement('script');
            script.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
            script.onload = resolve;
            document.head.appendChild(script);
        });
    },

    getUserCoordinates: async function () {
        if (!navigator.geolocation) {
            return null;
        }
        return new Promise((resolve) => {
            navigator.geolocation.getCurrentPosition(
                (pos) => resolve({ latitude: pos.coords.latitude, longitude: pos.coords.longitude }),
                (err) => resolve(null),
                { timeout: 4000, enableHighAccuracy: true }
            );
        });
    },

    initMap: async function (elementId, initialLat, initialLon, zoom = 14, dotNetHelper = null) {
        await this.ensureLeaflet();

        const container = document.getElementById(elementId);
        if (!container) return;

        // Cleanup existing instance if any
        if (this.mapInstances[elementId]) {
            try {
                this.mapInstances[elementId].map.remove();
            } catch { }
            delete this.mapInstances[elementId];
        }

        // 100% Free OpenStreetMap & OpenTopoMap Tile Providers (Zero API Keys Needed)
        const osmDark = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank">OpenStreetMap</a> contributors',
            className: 'noc-dark-tiles',
            maxZoom: 19
        });

        const osmStandard = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank">OpenStreetMap</a> contributors',
            maxZoom: 19
        });

        const topoMap = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank">OpenStreetMap</a> | &copy; <a href="https://opentopomap.org" target="_blank">OpenTopoMap</a>',
            maxZoom: 17
        });

        const map = L.map(elementId, {
            center: [initialLat, initialLon],
            zoom: zoom,
            layers: [osmDark],
            zoomControl: true,
            attributionControl: true
        });

        const baseMaps = {
            "🌙 Dark NOC Mode (Free OSM)": osmDark,
            "🗺️ Standard OpenStreetMap (Free)": osmStandard,
            "🏔️ Topo & Elevation (Free)": topoMap
        };

        L.control.layers(baseMaps, null, { position: 'topright' }).addTo(map);

        this.mapInstances[elementId] = {
            map: map,
            userMarker: null,
            servingMarker: null,
            towerMarkers: [],
            deviceMarkers: [],
            trailPolyline: null
        };

        // Try getting real browser location
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                async (pos) => {
                    const realLat = pos.coords.latitude;
                    const realLon = pos.coords.longitude;
                    map.setView([realLat, realLon], 14);

                    const entry = window.cellScopeMap.mapInstances[elementId];
                    if (entry && entry.userMarker) {
                        entry.userMarker.setLatLng([realLat, realLon]);
                    }

                    if (dotNetHelper) {
                        try {
                            await dotNetHelper.invokeMethodAsync('OnLocationUpdated', realLat, realLon);
                        } catch { }
                    }
                },
                (err) => {
                    // Fallback to default coordinates
                },
                { timeout: 5000, enableHighAccuracy: true }
            );
        }

        setTimeout(() => map.invalidateSize(), 250);
    },

    locateUser: function (elementId) {
        const entry = this.mapInstances[elementId];
        if (!entry || !entry.map) return;

        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition((pos) => {
                const lat = pos.coords.latitude;
                const lon = pos.coords.longitude;
                entry.map.flyTo([lat, lon], 15);
                if (entry.userMarker) {
                    entry.userMarker.setLatLng([lat, lon]);
                }
            });
        }
    },

    updateMap: function (elementId, data) {
        const entry = this.mapInstances[elementId];
        if (!entry || !entry.map) return;
        const map = entry.map;

        // 1. User Marker (Cyan Glowing Beacon)
        if (data.userLat && data.userLon) {
            const userIcon = L.divIcon({
                className: 'custom-user-marker',
                html: `<div style="width:20px;height:20px;background:#06b6d4;border:3px solid #ffffff;border-radius:50%;box-shadow:0 0 16px #06b6d4, 0 0 4px #06b6d4;display:flex;align-items:center;justify-content:center;"><div style="width:6px;height:6px;background:#fff;border-radius:50%;"></div></div>`,
                iconSize: [20, 20],
                iconAnchor: [10, 10]
            });

            if (entry.userMarker) {
                entry.userMarker.setLatLng([data.userLat, data.userLon]);
            } else {
                entry.userMarker = L.marker([data.userLat, data.userLon], { icon: userIcon })
                    .addTo(map)
                    .bindPopup(`<b>📍 Your Device Location</b><br><b>Latitude:</b> ${data.userLat.toFixed(5)}<br><b>Longitude:</b> ${data.userLon.toFixed(5)}<br><b>Accuracy:</b> High (GPS/Wi-Fi)`);
            }
        }

        // 2. Serving Cell Marker (Emerald Green)
        if (data.servingCell) {
            const sLat = data.servingCell.latitude || (data.userLat ? data.userLat + 0.0018 : null);
            const sLon = data.servingCell.longitude || (data.userLon ? data.userLon + 0.0018 : null);
            if (sLat && sLon) {
                const servingIcon = L.divIcon({
                    className: 'serving-cell-marker',
                    html: `<div style="background:#10b981;color:#0b0f19;padding:4px 9px;border-radius:6px;font-size:11px;font-weight:800;border:1.5px solid #ffffff;box-shadow:0 0 14px rgba(16,185,129,0.8);white-space:nowrap;display:inline-flex;align-items:center;gap:4px;">📡 ${data.servingCell.radioTechnology || 'Serving Cell'}</div>`,
                    iconAnchor: [40, 15]
                });

                if (entry.servingMarker) {
                    entry.servingMarker.setLatLng([sLat, sLon]);
                } else {
                    entry.servingMarker = L.marker([sLat, sLon], { icon: servingIcon })
                        .addTo(map)
                        .bindPopup(`<b>📡 Active Serving Cell</b><br><b>Cell ID:</b> ${data.servingCell.cellId || 'N/A'}<br><b>PCI:</b> ${data.servingCell.physicalCellId || 'N/A'}<br><b>Band:</b> ${data.servingCell.band || 'N/A'}<br><b>Signal:</b> ${data.servingCell.signalStrengthDbm || 'N/A'} dBm<br><b>Quality:</b> ${data.servingCell.signalQuality != null ? data.servingCell.signalQuality + ' dB' : 'Good'}`);
                }
            }
        }

        // 3. Known Public Telecom Towers (Amber / Gold Pins)
        if (data.towers && Array.isArray(data.towers)) {
            entry.towerMarkers.forEach(m => m.remove());
            entry.towerMarkers = [];

            data.towers.forEach(t => {
                const distText = t.distanceMeters ? ` • ${Math.round(t.distanceMeters)}m` : '';
                const towerIcon = L.divIcon({
                    className: 'tower-marker',
                    html: `<div style="background:#f59e0b;color:#0b0f19;padding:3px 8px;border-radius:6px;font-size:10.5px;font-weight:700;border:1px solid #ffffff;box-shadow:0 0 12px rgba(245,158,11,0.7);white-space:nowrap;display:inline-flex;align-items:center;gap:3px;">🗼 ${t.radioTechnology}${distText}</div>`,
                    iconAnchor: [42, 13]
                });

                const m = L.marker([t.latitude, t.longitude], { icon: towerIcon })
                    .addTo(map)
                    .bindPopup(`<b>🗼 Public Base Station / Tower</b><br><b>Cell ID:</b> ${t.cellId}<br><b>Operator:</b> ${t.operatorName || 'Public Carrier'}<br><b>Technology:</b> ${t.radioTechnology}<br><b>Physical Cell ID:</b> ${t.physicalCellId || 'N/A'}<br><b>Confidence:</b> ${t.confidence}<br><b>Distance:</b> ${t.distanceMeters ? Math.round(t.distanceMeters) + ' meters' : 'Nearby'}<br><b>Source:</b> ${t.source || 'OpenCellID Dataset'}<br><b>Verified:</b> ${t.lastVerified ? new Date(t.lastVerified).toLocaleDateString() : 'Active'}`);
                entry.towerMarkers.push(m);
            });
        }

        // 4. Connected Network Devices (Blue Badges)
        if (data.devices && Array.isArray(data.devices)) {
            entry.deviceMarkers.forEach(m => m.remove());
            entry.deviceMarkers = [];

            data.devices.forEach((d, idx) => {
                // Generate slight local coordinate scatter around user for visualization if no GPS
                const dLat = (data.userLat || 37.7749) + (Math.sin(idx * 1.3) * 0.0009);
                const dLon = (data.userLon || -122.4194) + (Math.cos(idx * 1.3) * 0.0009);

                const devIcon = L.divIcon({
                    className: 'device-marker',
                    html: `<div style="background:#3b82f6;color:#ffffff;padding:2px 6px;border-radius:5px;font-size:10px;font-weight:600;border:1px solid #ffffff;box-shadow:0 0 8px rgba(59,130,246,0.6);white-space:nowrap;">💻 ${d.hostname || d.ipAddress}</div>`,
                    iconAnchor: [30, 10]
                });

                const m = L.marker([dLat, dLon], { icon: devIcon })
                    .addTo(map)
                    .bindPopup(`<b>🌐 LAN Connected Device</b><br><b>Hostname:</b> ${d.hostname || 'Local Client'}<br><b>IP Address:</b> ${d.ipAddress}<br><b>Vendor:</b> ${d.vendor || 'Generic'}<br><b>Type:</b> ${d.deviceType || 'Client'}<br><b>Latency:</b> ${d.responseTimeMs || 1} ms`);
                entry.deviceMarkers.push(m);
            });
        }

        // 5. Trail Polyline
        if (data.trail && Array.isArray(data.trail) && data.trail.length > 1) {
            const latlngs = data.trail.map(pt => [pt.latitude, pt.longitude]);
            if (entry.trailPolyline) {
                entry.trailPolyline.setLatLngs(latlngs);
            } else {
                entry.trailPolyline = L.polyline(latlngs, {
                    color: '#06b6d4',
                    weight: 3,
                    opacity: 0.85,
                    dashArray: '4, 8'
                }).addTo(map);
            }
        }
    }
};

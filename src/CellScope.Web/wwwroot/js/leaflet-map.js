window.cellScopeMap = {
    mapInstances: {},

    ensureLeaflet: async function () {
        if (window.L) return;

        // Load CSS
        if (!document.getElementById('leaflet-css')) {
            const link = document.createElement('link');
            link.id = 'leaflet-css';
            link.rel = 'stylesheet';
            link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
            document.head.appendChild(link);
        }

        // Load JS
        await new Promise((resolve) => {
            const script = document.createElement('script');
            script.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
            script.onload = resolve;
            document.head.appendChild(script);
        });
    },

    initMap: async function (elementId, initialLat, initialLon, zoom = 14) {
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

        // 100% Free OpenStreetMap & CartoDB Tile Providers (Zero API Keys Needed)
        const osmStandard = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        });

        const osmDark = L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 19
        });

        const osmLight = L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 19
        });

        const map = L.map(elementId, {
            center: [initialLat, initialLon],
            zoom: zoom,
            layers: [osmDark],
            zoomControl: true,
            attributionControl: true
        });

        const baseMaps = {
            "🌙 Dark Mode (Free OSM)": osmDark,
            "🗺️ Standard OpenStreetMap (Free)": osmStandard,
            "☀️ Light Mode (Free OSM)": osmLight
        };

        L.control.layers(baseMaps, null, { position: 'topright' }).addTo(map);

        this.mapInstances[elementId] = {
            map: map,
            userMarker: null,
            servingMarker: null,
            neighborMarkers: [],
            towerMarkers: [],
            handoverMarkers: [],
            trailPolyline: null
        };

        // Try getting real browser location
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (pos) => {
                    const realLat = pos.coords.latitude;
                    const realLon = pos.coords.longitude;
                    map.setView([realLat, realLon], 14);
                    if (this.mapInstances[elementId].userMarker) {
                        this.mapInstances[elementId].userMarker.setLatLng([realLat, realLon]);
                    }
                },
                (err) => {
                    // Fallback to default coordinates
                },
                { timeout: 5000 }
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

        // 1. User Marker
        if (data.userLat && data.userLon) {
            const userIcon = L.divIcon({
                className: 'custom-user-marker',
                html: `<div style="width:18px;height:18px;background:#06b6d4;border:3px solid #fff;border-radius:50%;box-shadow:0 0 12px #06b6d4;"></div>`,
                iconSize: [18, 18],
                iconAnchor: [9, 9]
            });

            if (entry.userMarker) {
                entry.userMarker.setLatLng([data.userLat, data.userLon]);
            } else {
                entry.userMarker = L.marker([data.userLat, data.userLon], { icon: userIcon })
                    .addTo(map)
                    .bindPopup(`<b>Your Device Location</b><br>Lat: ${data.userLat.toFixed(5)}<br>Lon: ${data.userLon.toFixed(5)}`);
            }
        }

        // 2. Serving Cell Marker
        if (data.servingCell) {
            const sLat = data.servingCell.latitude || (data.userLat ? data.userLat + 0.0015 : null);
            const sLon = data.servingCell.longitude || (data.userLon ? data.userLon + 0.0015 : null);
            if (sLat && sLon) {
                const servingIcon = L.divIcon({
                    className: 'serving-cell-marker',
                    html: `<div style="background:#10b981;color:#0b0f19;padding:4px 8px;border-radius:6px;font-size:11px;font-weight:bold;border:1px solid #fff;box-shadow:0 0 12px #10b981;white-space:nowrap;">📡 ${data.servingCell.radioTechnology || 'Serving Cell'}</div>`,
                    iconAnchor: [35, 15]
                });

                if (entry.servingMarker) {
                    entry.servingMarker.setLatLng([sLat, sLon]);
                } else {
                    entry.servingMarker = L.marker([sLat, sLon], { icon: servingIcon })
                        .addTo(map)
                        .bindPopup(`<b>Serving Cell</b><br>Cell ID: ${data.servingCell.cellId || 'N/A'}<br>PCI: ${data.servingCell.physicalCellId || 'N/A'}<br>Band: ${data.servingCell.band || 'N/A'}<br>Signal: ${data.servingCell.signalStrengthDbm || 'N/A'} dBm`);
                }
            }
        }

        // 3. Known Public Towers
        if (data.towers && Array.isArray(data.towers)) {
            entry.towerMarkers.forEach(m => m.remove());
            entry.towerMarkers = [];

            data.towers.forEach(t => {
                const towerIcon = L.divIcon({
                    className: 'tower-marker',
                    html: `<div style="background:#f59e0b;color:#0b0f19;padding:3px 7px;border-radius:6px;font-size:10px;font-weight:700;box-shadow:0 0 10px rgba(245,158,11,0.5);white-space:nowrap;">🗼 Tower (${t.radioTechnology})</div>`,
                    iconAnchor: [35, 12]
                });

                const m = L.marker([t.latitude, t.longitude], { icon: towerIcon })
                    .addTo(map)
                    .bindPopup(`<b>Public Tower Location</b><br><b>Cell ID:</b> ${t.cellId}<br><b>Operator:</b> ${t.operatorName || 'Public Carrier'}<br><b>Confidence:</b> ${t.confidence}<br><b>Source:</b> ${t.source}<br><b>Last Verified:</b> ${t.lastVerified ? new Date(t.lastVerified).toLocaleDateString() : 'N/A'}`);
                entry.towerMarkers.push(m);
            });
        }

        // 4. Trail Polyline
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

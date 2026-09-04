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

    resolveCoordinates: async function (timeoutMs = 5000) {
        // 1. Try High Accuracy Browser Geolocation (GPS / Wi-Fi)
        if (navigator.geolocation) {
            try {
                const pos = await new Promise((resolve, reject) => {
                    navigator.geolocation.getCurrentPosition(
                        (p) => resolve(p),
                        (err) => reject(err),
                        { timeout: timeoutMs, enableHighAccuracy: true, maximumAge: 30000 }
                    );
                });
                if (pos && pos.coords && pos.coords.latitude && pos.coords.longitude) {
                    return {
                        lat: pos.coords.latitude,
                        lon: pos.coords.longitude,
                        source: 'GPS / Wi-Fi Geolocation',
                        accuracy: pos.coords.accuracy || 10
                    };
                }
            } catch (err) {
                console.warn("Browser GPS geolocation unavailable or timed out, checking IP Geolocation fallback...", err);
            }
        }

        // 2. Fast IP Geolocation Fallback (free public providers)
        try {
            const resp = await fetch('https://ipapi.co/json/', { signal: AbortSignal.timeout(4000) });
            if (resp.ok) {
                const data = await resp.json();
                if (data.latitude && data.longitude) {
                    return {
                        lat: parseFloat(data.latitude),
                        lon: parseFloat(data.longitude),
                        city: data.city,
                        country: data.country_name,
                        source: `IP Geolocation (${data.city || 'Local Area'})`,
                        accuracy: 2500
                    };
                }
            }
        } catch (ipErr) {
            console.warn("ipapi.co fallback unreachable, trying secondary provider...", ipErr);
        }

        try {
            const resp2 = await fetch('https://ipwhois.app/json/', { signal: AbortSignal.timeout(4000) });
            if (resp2.ok) {
                const data2 = await resp2.json();
                if (data2.latitude && data2.longitude) {
                    return {
                        lat: parseFloat(data2.latitude),
                        lon: parseFloat(data2.longitude),
                        city: data2.city,
                        country: data2.country,
                        source: `IP Geolocation (${data2.city || 'Local Area'})`,
                        accuracy: 3000
                    };
                }
            }
        } catch (ipErr2) {
            console.warn("Secondary IP provider unreachable:", ipErr2);
        }

        return null;
    },

    getUserCoordinates: async function () {
        const loc = await this.resolveCoordinates(4000);
        return loc ? { latitude: loc.lat, longitude: loc.lon } : null;
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
            dotNetHelper: dotNetHelper,
            userMarker: null,
            servingMarker: null,
            towerMarkers: [],
            deviceMarkers: [],
            propagationLayers: [],
            trailPolyline: null
        };

        // Listen for Pan & Zoom (Viewport Movement) to dynamically load visible towers anywhere on Earth
        let moveDebounceTimer = null;
        map.on('moveend', () => {
            clearTimeout(moveDebounceTimer);
            moveDebounceTimer = setTimeout(() => {
                const center = map.getCenter();
                const bounds = map.getBounds();
                const northEast = bounds.getNorthEast();
                const southWest = bounds.getSouthWest();
                const cornerDist = map.distance(center, northEast);
                const radiusMeters = Math.min(60000, Math.max(1500, cornerDist));

                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnMapViewChanged', center.lat, center.lng, radiusMeters, {
                        north: northEast.lat,
                        east: northEast.lng,
                        south: southWest.lat,
                        west: southWest.lng
                    }).catch(() => {});
                }
            }, 300);
        });

        setTimeout(() => map.invalidateSize(), 250);

        // Auto-locate real user position in background (GPS first, then IP fallback)
        this.resolveCoordinates(4000).then(async (loc) => {
            if (loc) {
                const curEntry = window.cellScopeMap.mapInstances[elementId];
                if (curEntry && curEntry.map) {
                    curEntry.map.setView([loc.lat, loc.lon], 14);
                    if (curEntry.userMarker) {
                        curEntry.userMarker.setLatLng([loc.lat, loc.lon]);
                    }
                    if (dotNetHelper) {
                        try {
                            await dotNetHelper.invokeMethodAsync('OnLocationUpdated', loc.lat, loc.lon);
                        } catch { }
                    }
                }
            }
        });
    },

    flyToLocation: function (elementId, lat, lon, zoom = 14) {
        const entry = this.mapInstances[elementId];
        if (entry && entry.map) {
            entry.map.flyTo([lat, lon], zoom, { duration: 1.2 });
        }
    },

    selectTower: function (elementId, cellId) {
        const entry = this.mapInstances[elementId];
        if (entry && entry.dotNetHelper) {
            try {
                entry.dotNetHelper.invokeMethodAsync('OnTowerSelected', cellId);
            } catch { }
        }
    },

    selectDevice: function (elementId, ipAddress) {
        const entry = this.mapInstances[elementId];
        if (entry && entry.dotNetHelper) {
            try {
                entry.dotNetHelper.invokeMethodAsync('OnDeviceSelected', ipAddress);
            } catch { }
        }
    },

    toggleDevice: function (elementId, ipAddress) {
        const entry = this.mapInstances[elementId];
        if (entry && entry.dotNetHelper) {
            try {
                entry.dotNetHelper.invokeMethodAsync('OnDeviceToggleConnection', ipAddress);
            } catch { }
        }
    },

    locateUser: async function (elementId) {
        const entry = this.mapInstances[elementId];
        if (!entry || !entry.map) return null;

        const loc = await this.resolveCoordinates(6000);
        if (loc) {
            entry.map.flyTo([loc.lat, loc.lon], 15);
            if (entry.userMarker) {
                entry.userMarker.setLatLng([loc.lat, loc.lon]);
            }
            if (entry.dotNetHelper) {
                try {
                    await entry.dotNetHelper.invokeMethodAsync('OnLocationUpdated', loc.lat, loc.lon);
                } catch (err) {
                    console.error("Failed to notify Blazor of location update:", err);
                }
            }
            return loc;
        } else {
            console.warn("Could not determine user coordinates via GPS or IP.");
            return null;
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
                html: `<div style="width:22px;height:22px;background:#06b6d4;border:3px solid #ffffff;border-radius:50%;box-shadow:0 0 16px #06b6d4, 0 0 6px #06b6d4;display:flex;align-items:center;justify-content:center;"><div style="width:6px;height:6px;background:#fff;border-radius:50%;"></div></div>`,
                iconSize: [22, 22],
                iconAnchor: [11, 11]
            });

            if (entry.userMarker) {
                entry.userMarker.setLatLng([data.userLat, data.userLon]);
            } else {
                entry.userMarker = L.marker([data.userLat, data.userLon], { icon: userIcon })
                    .addTo(map)
                    .bindPopup(`<b>📍 Your Device Location</b><br><b>Latitude:</b> ${data.userLat.toFixed(5)}<br><b>Longitude:</b> ${data.userLon.toFixed(5)}<br><b>Accuracy:</b> High (GPS / Wi-Fi Active)`);
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

        // 3. Known Public Telecom Towers & Attached Connected Devices & Calls
        if (data.towers && Array.isArray(data.towers)) {
            entry.towerMarkers.forEach(m => m.remove());
            entry.towerMarkers = [];

            const isDemo = data.isDemoActive !== undefined ? data.isDemoActive : true;

            data.towers.forEach(t => {
                const totalUes = (t.totalConnectedDevices || t.TotalConnectedDevices || (isDemo ? 2480 : 0)).toLocaleString();
                const activeSessions = (t.activeDataSessions || t.ActiveDataSessions || (isDemo ? 2120 : 0)).toLocaleString();
                const voLteChannels = (t.voLteVoiceChannels || t.VoLteVoiceChannels || (isDemo ? 310 : 0)).toLocaleString();
                const throughput = (t.aggregateThroughputMbps || t.AggregateThroughputMbps || (isDemo ? 540 : 0));
                const devList = t.connectedDevices || t.ConnectedDevices || [];
                const callList = t.activeCalls || t.ActiveCalls || [];
                const distText = t.distanceMeters ? ` • ${Math.round(t.distanceMeters)}m` : '';

                const badgeHtml = isDemo 
                    ? `<span style="background:#0b0f19;color:#fbbf24;padding:1px 6px;border-radius:10px;font-size:9.5px;font-weight:800;">${totalUes} UEs</span>`
                    : `<span style="background:#0b0f19;color:#10b981;padding:1px 6px;border-radius:10px;font-size:9.5px;font-weight:800;">🔒 Real Cell</span>`;

                const towerIcon = L.divIcon({
                    className: 'tower-marker',
                    html: `<div style="background:${isDemo ? '#f59e0b' : '#10b981'};color:#0b0f19;padding:3px 8px;border-radius:6px;font-size:10.5px;font-weight:700;border:1px solid #ffffff;box-shadow:0 0 14px ${isDemo ? 'rgba(245,158,11,0.7)' : 'rgba(16,185,129,0.7)'};white-space:nowrap;display:inline-flex;align-items:center;gap:4px;">🗼 ${t.radioTechnology}${distText} ${badgeHtml}</div>`,
                    iconAnchor: [50, 13]
                });

                // Build connected devices & active calls HTML snippet
                let devSnippet = '';
                if (isDemo) {
                    let callSnippet = '';
                    if (callList.length > 0) {
                        callSnippet = `
                            <div style="background:rgba(16,185,129,0.1);border:1px solid rgba(16,185,129,0.3);padding:6px;border-radius:6px;margin-bottom:6px;font-size:10px;">
                                <div style="font-weight:800;color:#10b981;display:flex;justify-content:space-between;">
                                    <span>📞 Ongoing Calls (${callList.length} Active):</span>
                                    <span>VoNR / VoLTE</span>
                                </div>
                                <div style="margin-top:3px;display:flex;flex-direction:column;gap:2px;">
                                    ${callList.slice(0, 2).map(c => `
                                        <div style="font-size:9px;color:#f8fafc;display:flex;justify-content:space-between;">
                                            <span><b>${c.callerNumber || c.CallerNumber}</b> ➔ <b>${c.receiverNumber || c.ReceiverNumber}</b></span>
                                            <span style="color:#fbbf24;">${c.callType || c.CallType}</span>
                                        </div>
                                    `).join('')}
                                </div>
                            </div>
                        `;
                    }

                    devSnippet = `
                        <div style="margin-top:8px;padding-top:6px;border-top:1px solid #1f2d42;">
                            ${callSnippet}
                            <div style="background:rgba(245,158,11,0.1);border:1px solid rgba(245,158,11,0.3);padding:6px;border-radius:6px;margin-bottom:6px;font-size:10px;">
                                <div style="font-weight:800;color:#fbbf24;display:flex;justify-content:space-between;">
                                    <span>📱 Attached Sector Load:</span>
                                    <span>${totalUes} Connected UEs</span>
                                </div>
                                <div style="color:#94a3b8;font-size:9.5px;margin-top:2px;">
                                    Data Bearers: <b>${activeSessions}</b> • Voice: <b>${voLteChannels}</b> • <b>${throughput} Mbps</b> DL
                                </div>
                            </div>
                            <div style="font-size:10px;font-weight:700;color:#f59e0b;text-transform:uppercase;margin-bottom:4px;">Sample Subscribers (${devList.length} Nodes):</div>
                            <div style="display:flex;flex-direction:column;gap:4px;max-height:100px;overflow-y:auto;">
                                ${devList.slice(0, 4).map(dev => `
                                    <div style="background:#111827;padding:3px 6px;border-radius:4px;border:1px solid #1f2d42;font-size:10px;">
                                        <div style="display:flex;justify-content:space-between;font-weight:600;color:#f8fafc;">
                                            <span>${dev.deviceName || dev.DeviceName}</span>
                                            <span style="color:${dev.signalColor || dev.SignalColor || '#10b981'}">${dev.signalStrengthDbm || dev.SignalStrengthDbm} dBm</span>
                                        </div>
                                        <div style="display:flex;justify-content:space-between;color:#94a3b8;font-size:9px;">
                                            <span>${dev.deviceType || dev.DeviceType} ${dev.phoneNumber ? `• 📱 ${dev.phoneNumber}` : ''}</span>
                                            <span>${dev.estimatedDistanceMeters || 200}m</span>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    `;
                } else {
                    // Strict Real-Only Mode
                    devSnippet = `
                        <div style="margin-top:8px;padding-top:6px;border-top:1px solid #1f2d42;">
                            <div style="background:rgba(16,185,129,0.1);border:1px solid rgba(16,185,129,0.3);padding:6px;border-radius:6px;margin-bottom:6px;font-size:10px;">
                                <div style="font-weight:800;color:#10b981;">🔒 Strict Real-Only Mode Active</div>
                                <div style="color:#94a3b8;font-size:9px;margin-top:2px;">
                                    Public telecom base station registered on OpenCellID/MLS. 3GPP air encryption prevents public eavesdropping.
                                </div>
                            </div>
                            <div style="font-size:10px;font-weight:700;color:#10b981;margin-bottom:4px;">
                                Paired Hardware Collectors: <b>${devList.length}</b> Node(s)
                            </div>
                        </div>
                    `;
                }

                const buttonText = isDemo ? `⚡ Inspect All ${totalUes} UEs & Ongoing Calls` : `🔍 Inspect Base Station Telemetry`;
                const buttonBg = isDemo ? '#f59e0b' : '#10b981';

                const popupHtml = `
                    <div style="min-width:250px;">
                        <b>🗼 Macro Base Station / Cellular Tower</b><br>
                        <b>Cell ID:</b> <span style="font-family:monospace;color:#06b6d4;">${t.cellId}</span><br>
                        <b>Operator:</b> ${t.operatorName || 'Telecom Carrier'}<br>
                        <b>Radio Technology:</b> <span style="color:${isDemo ? '#f59e0b' : '#10b981'};font-weight:700;">${t.radioTechnology}</span><br>
                        <b>Physical Cell ID (PCI):</b> ${t.physicalCellId || 'N/A'}<br>
                        <b>Distance:</b> ${t.distanceMeters ? Math.round(t.distanceMeters) + ' meters' : 'Nearby'}<br>
                        ${devSnippet}
                        <button onclick="window.cellScopeMap.selectTower('${elementId}', '${t.cellId}')" style="margin-top:8px;width:100%;background:${buttonBg};color:#0b0f19;border:none;border-radius:5px;padding:6px 8px;font-weight:800;font-size:11px;cursor:pointer;">
                            ${buttonText}
                        </button>
                    </div>
                `;

                const m = L.marker([t.latitude, t.longitude], { icon: towerIcon })
                    .addTo(map)
                    .bindPopup(popupHtml, { maxWidth: 300 });

                m.on('click', () => {
                    window.cellScopeMap.selectTower(elementId, t.cellId);
                });

                entry.towerMarkers.push(m);
            });
        }

        // 4. Connected Local Area Network (LAN) Devices
        if (data.devices && Array.isArray(data.devices)) {
            entry.deviceMarkers.forEach(m => m.remove());
            entry.deviceMarkers = [];

            const userBaseLat = data.userLat || 37.7749;
            const userBaseLon = data.userLon || -122.4194;

            data.devices.forEach((d, idx) => {
                const isOnline = d.isOnline !== undefined ? d.isOnline : (d.IsOnline !== undefined ? d.IsOnline : true);
                const angle = (idx * 2 * Math.PI) / Math.max(1, data.devices.length) + (idx * 0.4);
                const radiusDist = 0.00045 + (idx * 0.00015);
                const dLat = userBaseLat + (Math.sin(angle) * radiusDist);
                const dLon = userBaseLon + (Math.cos(angle) * radiusDist);

                let iconSymbol = "💻";
                const devType = d.deviceType || d.DeviceType || "";
                if (devType === "Router") iconSymbol = "🌐";
                else if (devType === "AccessPoint") iconSymbol = "📶";
                else if (devType === "Phone") iconSymbol = "📱";
                else if (devType === "TV") iconSymbol = "📺";
                else if (devType === "IoT") iconSymbol = "📡";
                else if (devType === "Server") iconSymbol = "🖧";
                else if (devType === "Printer") iconSymbol = "🖨️";

                const bgStyle = isOnline ? "background:#3b82f6;color:#ffffff;box-shadow:0 0 12px rgba(59,130,246,0.8);" : "background:#475569;color:#94a3b8;box-shadow:none;border-color:#64748b;opacity:0.75;";
                const statusBadge = isOnline ? `<span style="background:#10b981;color:#0b0f19;padding:1px 5px;border-radius:10px;font-size:9px;font-weight:800;">ONLINE</span>` : `<span style="background:#ef4444;color:#ffffff;padding:1px 5px;border-radius:10px;font-size:9px;font-weight:800;">DISCONNECTED</span>`;

                const devIcon = L.divIcon({
                    className: 'device-marker',
                    html: `<div style="${bgStyle}padding:3px 7px;border-radius:6px;font-size:10.5px;font-weight:700;border:1.5px solid #ffffff;white-space:nowrap;display:inline-flex;align-items:center;gap:4px;">${iconSymbol} ${d.hostname || d.Hostname || d.ipAddress || d.IpAddress} ${statusBadge}</div>`,
                    iconAnchor: [45, 11]
                });

                const actionButton = isOnline ? `
                    <button onclick="window.cellScopeMap.toggleDevice('${elementId}', '${d.ipAddress || d.IpAddress}')" style="background:rgba(239,68,68,0.2);color:#ef4444;border:1px solid #ef4444;border-radius:5px;padding:4px 8px;font-weight:700;font-size:10.5px;cursor:pointer;flex:1;">
                        🛑 Disconnect
                    </button>
                ` : `
                    <button onclick="window.cellScopeMap.toggleDevice('${elementId}', '${d.ipAddress || d.IpAddress}')" style="background:rgba(16,185,129,0.2);color:#10b981;border:1px solid #10b981;border-radius:5px;padding:4px 8px;font-weight:700;font-size:10.5px;cursor:pointer;flex:1;">
                        🟢 Connect
                    </button>
                `;

                const phoneLine = (d.phoneNumber || d.PhoneNumber) ? `<b>Mobile / MSISDN:</b> <span style="font-family:monospace;color:#06b6d4;">${d.phoneNumber || d.PhoneNumber}</span><br>` : '';

                const devPopup = `
                    <div style="min-width:230px;">
                        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:4px;">
                            <b>${iconSymbol} ${d.hostname || d.Hostname || 'Local Client'}</b>
                            ${statusBadge}
                        </div>
                        <b>IP Address:</b> <span style="font-family:monospace;color:#06b6d4;">${d.ipAddress || d.IpAddress}</span><br>
                        ${phoneLine}
                        <b>MAC Address:</b> <span style="font-family:monospace;color:#94a3b8;font-size:10px;">${d.macAddress || d.MacAddress || 'Restricted on OS'}</span><br>
                        <b>Vendor / OEM:</b> ${d.vendor || d.Vendor || 'Generic Device'}<br>
                        <b>Device Type:</b> <span style="background:#111827;padding:1px 5px;border-radius:4px;">${d.deviceType || d.DeviceType}</span><br>
                        <b>Band & Speed:</b> <span style="font-size:10.5px;color:#cbd5e1;">${d.connectionBand || d.ConnectionBand || 'Wi-Fi 6'} (${d.linkSpeedMbps || d.LinkSpeedMbps || 1200} Mbps)</span><br>
                        <b>Ping Latency:</b> ${d.responseTimeMs || d.ResponseTimeMs || 1} ms<br>
                        <b>Identified Services:</b> <span style="font-size:10px;color:#94a3b8;">${d.safeServiceSummary || d.SafeServiceSummary || 'ICMP Host'}</span><br>
                        <div style="display:flex;gap:6px;margin-top:8px;">
                            ${actionButton}
                            <button onclick="window.cellScopeMap.selectDevice('${elementId}', '${d.ipAddress || d.IpAddress}')" style="background:#3b82f6;color:#ffffff;border:none;border-radius:5px;padding:4px 8px;font-weight:700;font-size:10.5px;cursor:pointer;flex:1;">
                                🔍 Inspect
                            </button>
                        </div>
                    </div>
                `;

                const m = L.marker([dLat, dLon], { icon: devIcon })
                    .addTo(map)
                    .bindPopup(devPopup, { maxWidth: 280 });

                m.on('click', () => {
                    window.cellScopeMap.selectDevice(elementId, d.ipAddress || d.IpAddress);
                });

                entry.deviceMarkers.push(m);
            });
        }

        // 5. Trail Polyline
        if (data.trail && Array.isArray(data.trail) && data.trail.length > 1) {
            const latlngs = data.trail.map(pt => [pt.latitude || pt.Latitude, pt.longitude || pt.Longitude]);
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
    },

    renderPropagationLayers: function (elementId, propData) {
        const entry = this.mapInstances[elementId];
        if (!entry || !entry.map || !propData) return;
        const map = entry.map;

        // Clear existing propagation layers
        if (entry.propagationLayers) {
            entry.propagationLayers.forEach(l => l.remove());
            entry.propagationLayers = [];
        } else {
            entry.propagationLayers = [];
        }

        const centerLat = propData.centerLatitude || propData.CenterLatitude;
        const centerLon = propData.centerLongitude || propData.CenterLongitude;
        if (!centerLat || !centerLon) return;

        // 1. Draw Concentric RSRP Decay Contour Rings
        const rings = propData.contourRings || propData.ContourRings || [];
        // Draw in reverse order so larger outer rings don't mask smaller inner rings
        rings.slice().reverse().forEach(ring => {
            const radius = ring.radiusMeters || ring.RadiusMeters;
            const color = ring.colorHex || ring.ColorHex || '#10b981';
            const opacity = ring.fillOpacity || ring.FillOpacity || 0.15;
            const rating = ring.rating || ring.Rating || '';

            const circle = L.circle([centerLat, centerLon], {
                radius: radius,
                color: color,
                weight: 1.5,
                opacity: 0.7,
                fillColor: color,
                fillOpacity: opacity,
                dashArray: '3, 6'
            }).addTo(map).bindPopup(`<b>📡 Signal Coverage Contour</b><br><b>Rating:</b> ${rating}<br><b>Radius:</b> ${radius}m`);

            entry.propagationLayers.push(circle);
        });

        // 2. Draw 3 Sector Antenna Beam Polygons (65-degree horizontal beamwidth)
        const sectors = propData.sectors || propData.Sectors || [];
        sectors.forEach(sec => {
            const coords = sec.polygonGeoJsonCoordinates || sec.PolygonGeoJsonCoordinates || [];
            const secName = sec.sectorName || sec.SectorName || 'Sector';
            const azimuth = sec.azimuthDegrees || sec.AzimuthDegrees || 0;
            const gain = sec.mainLobeGainDbi || sec.MainLobeGainDbi || 18;

            if (coords && coords.length > 2) {
                const poly = L.polygon(coords, {
                    color: '#38bdf8',
                    weight: 2,
                    opacity: 0.9,
                    fillColor: '#0284c7',
                    fillOpacity: 0.22
                }).addTo(map).bindPopup(`<b>🗼 Antenna Sector Pattern</b><br><b>Name:</b> ${secName}<br><b>Azimuth:</b> ${azimuth}°<br><b>Beamwidth:</b> 65° Horizontal<br><b>Gain:</b> ${gain} dBi`);

                entry.propagationLayers.push(poly);
            }
        });
    },

    clearPropagationLayers: function (elementId) {
        const entry = this.mapInstances[elementId];
        if (!entry || !entry.propagationLayers) return;
        entry.propagationLayers.forEach(l => l.remove());
        entry.propagationLayers = [];
    }
};


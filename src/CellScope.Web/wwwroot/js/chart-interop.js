window.cellScopeCharts = {
    renderSignalTrend: function (containerId, points, label = "Signal Strength (dBm)", color = "#06b6d4") {
        const container = document.getElementById(containerId);
        if (!container || !points || points.length === 0) return;

        const width = container.clientWidth || 500;
        const height = container.clientHeight || 180;
        const padL = 45, padR = 20, padT = 20, padB = 30;
        const plotW = width - padL - padR;
        const plotH = height - padT - padB;

        const values = points.map(p => p.value);
        const minVal = -120;
        const maxVal = -50;

        const getX = (i) => padL + (i / Math.max(1, points.length - 1)) * plotW;
        const getY = (v) => padT + (1 - (v - minVal) / (maxVal - minVal)) * plotH;

        let pathD = "";
        points.forEach((p, i) => {
            const x = getX(i);
            const y = getY(p.value);
            pathD += (i === 0 ? `M ${x} ${y}` : ` L ${x} ${y}`);
        });

        // Area fill
        const areaD = `${pathD} L ${getX(points.length - 1)} ${padT + plotH} L ${padL} ${padT + plotH} Z`;

        const svg = `
        <svg width="100%" height="100%" viewBox="0 0 ${width} ${height}" style="overflow:visible;font-family:sans-serif;">
            <defs>
                <linearGradient id="grad-${containerId}" x1="0%" y1="0%" x2="0%" y2="100%">
                    <stop offset="0%" stop-color="${color}" stop-opacity="0.35"/>
                    <stop offset="100%" stop-color="${color}" stop-opacity="0.0"/>
                </linearGradient>
            </defs>
            <!-- Grid Lines -->
            <line x1="${padL}" y1="${getY(-70)}" x2="${padL + plotW}" y2="${getY(-70)}" stroke="#1f2d42" stroke-dasharray="3,3"/>
            <text x="${padL - 6}" y="${getY(-70) + 3}" fill="#64748b" font-size="10" text-anchor="end">-70</text>

            <line x1="${padL}" y1="${getY(-85)}" x2="${padL + plotW}" y2="${getY(-85)}" stroke="#1f2d42" stroke-dasharray="3,3"/>
            <text x="${padL - 6}" y="${getY(-85) + 3}" fill="#64748b" font-size="10" text-anchor="end">-85</text>

            <line x1="${padL}" y1="${getY(-100)}" x2="${padL + plotW}" y2="${getY(-100)}" stroke="#1f2d42" stroke-dasharray="3,3"/>
            <text x="${padL - 6}" y="${getY(-100) + 3}" fill="#64748b" font-size="10" text-anchor="end">-100</text>

            <!-- Area & Line -->
            <path d="${areaD}" fill="url(#grad-${containerId})" />
            <path d="${pathD}" fill="none" stroke="${color}" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" />

            <!-- End point dot -->
            ${points.length > 0 ? `
                <circle cx="${getX(points.length - 1)}" cy="${getY(points[points.length - 1].value)}" r="4.5" fill="${color}" stroke="#fff" stroke-width="1.5" />
            ` : ''}

            <!-- Time labels -->
            <text x="${padL}" y="${height - 8}" fill="#64748b" font-size="10">${points[0].label || ''}</text>
            <text x="${padL + plotW}" y="${height - 8}" fill="#64748b" font-size="10" text-anchor="end">${points[points.length - 1].label || ''}</text>
        </svg>
        `;

        container.innerHTML = svg;
    }
};

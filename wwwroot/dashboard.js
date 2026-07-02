function drawLineChart(canvas, labels, values, unit) {
    const devicePixelRatio = window.devicePixelRatio || 1;
    const rectangle = canvas.parentElement.getBoundingClientRect();
    canvas.width = rectangle.width * devicePixelRatio;
    canvas.height = rectangle.height * devicePixelRatio;
    canvas.style.width = rectangle.width + 'px';
    canvas.style.height = rectangle.height + 'px';


    const context = canvas.getContext('2d'); // it is a built-in browser API to get the drawing context from <canvas> element 
    context.scale(devicePixelRatio, devicePixelRatio);

    const Width = rectangle.width, Height = rectangle.height;
    const padding = {top: 16, right: 16, bottom: 40, left: 52 };
    const chartW = Width - padding.left - padding.right;
    const chartH = Height - padding.top - padding.bottom;

    context.clearRect(0, 0, Width, Height);

    if (!values.length) {
        context.fillStyle = '#7d8590';
        context.font = '13px monospace';
        context.textAlign = 'center';
        context.fillText('No data yet', Width /2, Height / 2);
        return;
    }

    const max = Math.max(... values) * 1.15 || 1;

    // Grid lines and y-axis labels
    context.strokeStyle = '#21262d';
    context.lineWidth = 1;

    // Lopp to draw 5 horizontal grid lines across the chart, evenly spaced from top to bottom
    for (let i = 0; i <= 4; i++) {
        const y = padding.top + (chartH / 4) * i;
        context.beginPath();
        context.moveTo(padding.left, y);
        context.lineTo(padding.left + chartW, y);
        context.stroke();

        const val = Math.round(max - (max / 4) * i);
        context.fillStyle = '#7d8590';
        context.font = "10px 'SF Mono', monospace";
        context.textAlign = 'right';
        context.fillText(val + (unit || ''), padding.left - 6, y + 3);
    }

       // X-axis labels
    const step = Math.ceil(values.length / 8);
    values.forEach((v, i) => {
        if (i % step === 0 && labels[i]) {
            const x = padding.left + (chartW / (values.length - 1)) * i;
            context.fillStyle = '#7d8590';
            context.font = "9px 'SF Mono', monospace";
            context.textAlign = 'center';
            context.fillText(labels[i].slice(11, 16), x, padding.top + chartH + 14);
        }
    });

    // Line path
    context.beginPath();
    context.strokeStyle = '#2f81f7';
    context.lineWidth = 2;
    context.lineJoin = 'round';
    values.forEach((v, i) => {
        const x = padding.left + (chartW / (values.length - 1)) * i;
        const y = padding.top + chartH - (v / max) * chartH;
        i === 0 ? context.moveTo(x, y) : context.lineTo(x, y);
    });
    context.stroke();

    // Fill under the line
    context.lineTo(padding.left + chartW, padding.top + chartH);
    context.lineTo(padding.left, padding.top + chartH);
    context.closePath();
    context.fillStyle = 'rgba(47, 129, 247, 0.08)';
    context.fill();

    // Dots on data points
    values.forEach((v, i) => {
        const x = padding.left + (chartW / (values.length - 1)) * i;
        const y = padding.top + chartH - (v / max) * chartH;
        context.beginPath();
        context.arc(x, y, 3, 0, Math.PI * 2);
        context.fillStyle = '#2f81f7';
        context.fill();
    });
}


// Fetches JSON from a given URL and returns it as a JavaScript object
// Throws an error if the HTTP response is not successful (non 2xx status code)
async function fetchJson(url) {
    const response = await fetch(url); // Built-in browser API to make an HTTP GET request
    if (!response.ok) throw new Error(`${url} → ${response.status}`); // Throw if request failed
    return response.json(); // Convert raw response body into a usable JS object
}

// Converts a raw latency number (ms) into a readable string
// e.g. 2341 → "2.3s", 841 → "841ms", null → "—"
function formatLatency(ms) {
    if (ms == null || ms === '' || ms === '—') return '—'; // Return dash if value is missing
    const n = Number(ms); // Convert to number in case it came in as a string
    return isNaN(n) ? '—' : n >= 1000 ? (n / 1000).toFixed(1) + 's' : n + 'ms';
    // If not a number → "—"
    // If >= 1000ms → convert to seconds e.g. "2.3s"
    // Otherwise → show as milliseconds e.g. "841ms"
}

// Converts an ISO timestamp e.g. "2026-06-30T14:32:11" into a short "14:32" time string
function formatTime(iso) {
    if (!iso) return '—'; // Return dash if value is missing
    try {
        return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        // Parse ISO string into a Date object, then format as HH:MM using browser locale
    } catch {
        return iso.slice(11, 16); // Fallback: directly slice HH:MM from the ISO string
    }
}


function renderSummary(summary) {
    document.getElementById('m-total').textContent = summary.totalExtractions ?? '-';
    document.getElementById('m-today').textContent = summary.extractionsToday  ?? '-';
    document.getElementById('m-latency').textContent = formatLatency(summary.averageLatencyMs);
    document.getElementById('m-comparisons').textContent = summary.totalComparisons ?? '—';
    document.getElementById('m-changed').textContent = summary.totalFieldsChanged ?? '-';

    const rate = summary.totalComparisons > 0 
        ? ((summary.fullMatchCount / summary.totalComparisons) * 100).toFixed(1) + '%' : '-';

    const matchElement = document.getElementById('m-matchrate');
    matchElement.textContent = rate;

    const percentage = parseFloat(rate);
    matchElement.className = 'metric-value ' + (isNaN(percentage) ? 'green' : percentage >= 80 ? 'green' : percentage >= 50 ? 'yellow' : 'red');    
}

function renderLogs(logs) {
    const list = document.getElementById('logList');
    document.getElementById('logCount').textContent = logs.length + ' entries';

    if (!logs.length) {
        list.innerHTML = '<div class="empty-state"><div class="empty-icon">○</div>No extractions yet.</div>';
        return;
    }

     list.innerHTML = logs.map(log => `
        <div class="log-item">
            <div class="log-row">
                <span class="log-meta">${formatTime(log.timestamp)}</span>
                <span class="log-latency">${formatLatency(log.latencyMs)}</span>
            </div>
            <div class="log-row">
                <span class="log-meta">${log.doneReason || '—'}</span>
            </div>
        </div>
    `).join('');
}

async function renderChart() {
    const canvas = document.getElementById('latencyChart');
    const data = await fetchJson('/api/Dashboard/latency-trend');
    document.getElementById('chartBadge').textContent = `last ${data.length} extractions`;
    drawLineChart(
        canvas,
        data.map(d => d.timestamp),
        data.map(d => Number(d.latencyMs)),
        'ms'
    );
}

async function refreshAll() {
    try {
        const [summary, logs] = await Promise.all([
            fetchJson('/api/Dashboard/report-json'),
            fetchJson('/api/Dashboard/recent-logs'),
        ]);
        renderSummary(summary);
        renderLogs(logs);
        await renderChart();
        document.getElementById('lastRefresh').textContent = new Date().toLocaleTimeString();
    } catch (err) {
        console.error('Dashboard refresh failed:', err);
    }
}

// Redraw chart on window resize so it stays sharp and correctly sized
window.addEventListener('resize', refreshAll);

refreshAll();
setInterval(refreshAll, 8000);

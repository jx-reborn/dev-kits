namespace JXReborn.DualParityServer.Web;

/// <summary>
/// Self-contained offline HTML/CSS/JS dashboard for live Dual-Run Parity monitoring and control.
/// </summary>
public static class DashboardHtml
{
    public const string Page = """
<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width,initial-scale=1" />
<title>JXReborn Dual-Run Parity Server</title>
<style>
  :root { --bg:#0b1220; --panel:#111c33; --border:#243454; --text:#e2e8f0; --muted:#94a3b8; --accent:#38bdf8; --ok:#34d399; --warn:#fbbf24; --crit:#f87171; }
  * { box-sizing:border-box; }
  body { margin:0; font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif; background:var(--bg); color:var(--text); }
  header { display:flex; align-items:center; justify-content:space-between; padding:14px 20px; border-bottom:1px solid var(--border); background:#0d1629; }
  h1 { margin:0; font-size:18px; }
  .sub { color:var(--muted); font-size:12px; }
  main { padding:18px 20px; display:grid; gap:16px; }
  .row { display:grid; grid-template-columns:repeat(auto-fit,minmax(260px,1fr)); gap:14px; }
  .panel { background:var(--panel); border:1px solid var(--border); border-radius:10px; padding:14px; }
  .panel h2 { margin:0 0 10px; font-size:14px; color:var(--accent); text-transform:uppercase; letter-spacing:.04em; }
  .controls { display:flex; flex-wrap:wrap; gap:8px; align-items:center; }
  button { background:#1d4ed8; color:#fff; border:none; border-radius:7px; padding:8px 13px; cursor:pointer; font-weight:600; font-size:13px; }
  button:hover { background:#2563eb; }
  button.sec { background:#1e293b; border:1px solid var(--border); }
  button.sec:hover { background:#334155; }
  button.ok { background:#059669; }
  button.danger { background:#dc2626; }
  input { background:#0b1220; color:var(--text); border:1px solid var(--border); border-radius:6px; padding:7px 9px; font-size:13px; }
  .pill { display:inline-block; padding:2px 8px; border-radius:999px; font-size:11px; font-weight:700; }
  .pill.ok { background:#064e3b; color:#a7f3d0; }
  .pill.crit { background:#7f1d1d; color:#fecaca; }
  .pill.warn { background:#78350f; color:#fde68a; }
  .kv { display:grid; grid-template-columns:120px 1fr; gap:4px 8px; font-size:13px; }
  .kv div:nth-child(odd) { color:var(--muted); }
  table { width:100%; border-collapse:collapse; font-size:12.5px; }
  th, td { text-align:left; padding:8px 10px; border-bottom:1px solid var(--border); vertical-align:top; }
  th { color:var(--muted); font-size:11px; text-transform:uppercase; }
  code { background:#0b1220; padding:2px 5px; border-radius:4px; color:#93c5fd; font-size:12px; }
  .toast { font-size:12px; color:var(--ok); margin-left:8px; }
  pre { margin:6px 0 0; background:#0b1220; padding:8px; border-radius:6px; overflow:auto; font-size:12px; max-height:160px; white-space:pre-wrap; }
</style>
</head>
<body>
<header>
  <div>
    <h1>JXReborn Dual-Run Parity Server</h1>
    <div class="sub" id="sessionInfo">Loading...</div>
  </div>
  <div class="controls">
    <button class="ok" onclick="post('/api/mirror/start')">Start Interactive Mirror</button>
    <button class="danger" onclick="post('/api/mirror/stop')">Stop Mirror</button>
    <button onclick="post('/api/diff/now')">Diff Snapshot Now</button>
    <button class="sec" onclick="crawlNpcs()">Crawl Nearby NPCs</button>
    <button class="sec" onclick="exportReport()">Export Report</button>
    <span class="toast" id="toast"></span>
  </div>
</header>

<main>
  <div class="row">
    <div class="panel">
      <h2>Attach 2 Running game.exe (PID)</h2>
      <div class="controls">
        <input id="legacyPid" type="number" placeholder="Legacy PID" style="width:120px" />
        <input id="rebornPid" type="number" placeholder="Reborn PID" style="width:120px" />
        <button onclick="attachPids()">Attach PIDs</button>
      </div>
      <div style="margin-top:8px" class="controls">
        <input id="crawlFilter" type="text" placeholder="NPC filter (optional)" style="width:160px" />
        <input id="crawlBranches" type="number" value="0" min="0" max="10" style="width:70px" title="Max dialog branches per NPC" />
        <span class="sub">branches/NPC</span>
      </div>
    </div>

    <div class="panel">
      <h2>Legacy Target (jx-linux)</h2>
      <div class="kv" id="legacyInfo"></div>
      <pre id="legacyDialog">(no dialog)</pre>
    </div>

    <div class="panel">
      <h2>Reborn Target (reborn-server)</h2>
      <div class="kv" id="rebornInfo"></div>
      <pre id="rebornDialog">(no dialog)</pre>
    </div>
  </div>

  <div class="panel">
    <h2>Live Findings (Critical &amp; Warnings)</h2>
    <table>
      <thead><tr><th>ID</th><th>Severity</th><th>Category</th><th>Step</th><th>Field</th><th>Legacy</th><th>Reborn</th></tr></thead>
      <tbody id="findingsBody"><tr><td colspan="7" class="sub">No findings yet.</td></tr></tbody>
    </table>
  </div>

  <div class="panel">
    <h2>NPC Parity Scorecard</h2>
    <table>
      <thead><tr><th>Map</th><th>NPC</th><th>Pos</th><th>Branches</th><th>Critical</th><th>Warning</th><th>Status</th></tr></thead>
      <tbody id="npcBody"><tr><td colspan="7" class="sub">Run "Crawl Nearby NPCs" to populate.</td></tr></tbody>
    </table>
  </div>
</main>

<script>
const toast = (msg) => { document.getElementById('toast').textContent = msg; setTimeout(() => { if(document.getElementById('toast').textContent === msg) document.getElementById('toast').textContent = ''; }, 5000); };
const esc = (s) => String(s ?? '').replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));

async function post(url, body) {
  try {
    const r = await fetch(url, { method:'POST', headers:{'Content-Type':'application/json'}, body: body ? JSON.stringify(body) : undefined });
    const data = await r.json();
    if (!r.ok) { toast('Error: ' + (data.error || r.statusText)); return null; }
    toast(data.message || 'Done');
    refresh();
    return data;
  } catch (e) { toast('Error: ' + e.message); return null; }
}

function attachPids() {
  const legacyPid = Number(document.getElementById('legacyPid').value);
  const rebornPid = Number(document.getElementById('rebornPid').value);
  post('/api/clients/attach-pid', { legacyPid, rebornPid });
}

function crawlNpcs() {
  const npcNameFilter = document.getElementById('crawlFilter').value || null;
  const maxBranchesPerNpc = Number(document.getElementById('crawlBranches').value) || 0;
  post('/api/crawl/npcs', { npcNameFilter, maxBranchesPerNpc });
}

async function exportReport() {
  const res = await post('/api/report/export');
  if (res && res.paths) toast('Exported to: ' + res.paths.outputDirectory);
}

function renderTarget(elId, dlgId, info, proxy) {
  const el = document.getElementById(elId);
  const dlg = document.getElementById(dlgId);
  const proxyTxt = proxy ? `:${proxy.listenPort} -> ${proxy.upstreamHost}:${proxy.upstreamPort} (conn:${proxy.activeConnections}, C2S:${proxy.bytesClientToServer}B, S2C:${proxy.bytesServerToClient}B)` : 'disabled';
  if (!info || !info.connected) {
    el.innerHTML = `<div>Status</div><div><span class="pill crit">Disconnected</span></div><div>TCP Proxy</div><div><code>${esc(proxyTxt)}</code></div>`;
    dlg.textContent = '(disconnected)';
    return;
  }
  const p = info.player;
  el.innerHTML = `
    <div>Status</div><div><span class="pill ok">Connected</span> pipe <code>${esc(info.pipeName)}</code></div>
    <div>TCP Proxy</div><div><code>${esc(proxyTxt)}</code></div>
    <div>Player</div><div>${esc(p.name)} (Lv.${p.level}) | Map ${p.mapId} (${p.mpsX}, ${p.mpsY})</div>
    <div>Money / Exp</div><div>${p.money} | Exp ${p.exp} | Items: ${info.itemCount} | NPCs: ${info.npcCount}</div>`;
  const d = info.dialog;
  dlg.textContent = d && d.isOpen
    ? `[OPEN] ${d.prompt}\n` + (d.options || []).map((o, i) => `  [${i}] ${o}`).join('\n')
    : '(no dialog open)';
}

async function refresh() {
  try {
    const [st, fList, nList] = await Promise.all([
      fetch('/api/status').then(r => r.json()),
      fetch('/api/findings').then(r => r.json()),
      fetch('/api/npcs').then(r => r.json())
    ]);
    document.getElementById('sessionInfo').textContent =
      `Session: ${st.sessionId} | Mirror: ${st.mirrorRunning ? 'RUNNING' : 'IDLE'} (${st.mirrorStatus}) | Steps: ${st.totalSteps} (${st.passedSteps} pass) | Findings: ${st.criticalCount} crit, ${st.warningCount} warn`;
    renderTarget('legacyInfo', 'legacyDialog', st.legacy, st.legacyProxy);
    renderTarget('rebornInfo', 'rebornDialog', st.reborn, st.rebornProxy);

    const fb = document.getElementById('findingsBody');
    fb.innerHTML = fList.length === 0
      ? '<tr><td colspan="7" class="sub">No findings yet.</td></tr>'
      : fList.slice(-80).reverse().map(f => `<tr>
          <td><code>${esc(f.findingId)}</code></td>
          <td><span class="pill ${f.severity === 'Critical' ? 'crit' : 'warn'}">${esc(f.severity)}</span></td>
          <td>${esc(f.category)}</td>
          <td>#${f.stepIndex} ${esc(f.stepName)}</td>
          <td><code>${esc(f.fieldPath)}</code></td>
          <td>${esc(f.legacyValue)}</td>
          <td>${esc(f.rebornValue)}</td>
        </tr>`).join('');

    const nb = document.getElementById('npcBody');
    nb.innerHTML = nList.length === 0
      ? '<tr><td colspan="7" class="sub">Run "Crawl Nearby NPCs" to populate.</td></tr>'
      : nList.map(n => `<tr>
          <td>${n.mapId}</td><td>${esc(n.npcName)}</td><td>(${n.mpsX}, ${n.mpsY})</td>
          <td>${n.branchesTested}</td><td>${n.criticalFindings}</td><td>${n.warningFindings}</td>
          <td><span class="pill ${n.matched ? 'ok' : 'crit'}">${n.matched ? 'PASS' : 'FAIL'}</span> ${esc(n.summary)}</td>
        </tr>`).join('');
  } catch (e) { /* retry on next tick */ }
}

refresh();
setInterval(refresh, 1500);
</script>
</body>
</html>
""";
}

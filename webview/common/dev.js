// 開発用(ブラウザで ?dev を付けて開いたときだけ読む): アプリの代わりに jmasHost を呼ぶ小さなパネル。
//   ・模擬の計測(Full / Standard / Quaternion、100 Hz)… アプリと同じく 0.05 秒ごとに push する
//   ・CSV を開く … ファイルを選ぶと blob URL を openReplay に渡す(アプリは仮想ホストの URL を渡す)
//   ・アプリの受け口の真似 … start の features(notify / records)を渡し、ページから届いた notify / table / records を
//     アプリと同じ規則で検査して、パネルに出す。表は Stop の後に「Download tables」で CSV に落とせる(アプリが書くのと同じ形)。
//     仕様は webview/proposals/detector-events-and-records.md(確定したら BRIDGE.md)。
// アプリへの post はブラウザでは console に出る(bridge.js の post が jmasDevSink を呼ぶ)。
const host = window.jmasHost;
const panel = document.createElement("div");
panel.style.cssText = "max-width:100%;box-sizing:border-box;overflow:hidden;display:flex;flex-wrap:wrap;gap:8px;align-items:center;padding:8px;border:1px dashed var(--line);border-radius:8px;margin-bottom:8px;font-size:12px";
panel.innerHTML = `<b>Dev</b>
  <select id="devMode"><option>full</option><option>standard</option><option>quaternion</option></select>
  <button id="devStart" type="button">Start simulated measurement</button>
  <button id="devStop" type="button">Stop</button>
  <button id="devGap" type="button">Gap</button>
  <button id="devMark" type="button">Free Marking</button>
  <label title="start の features に notify / records を入れる(外すと、未対応のアプリの真似)"><input id="devFeatures" type="checkbox" checked> App features</label>
  <button id="devTables" type="button" disabled>Download tables</button>
  <label style="max-width:100%">Open CSV <input id="devCsv" type="file" accept=".csv,.gz" style="max-width:100%"></label>`;
document.body.prepend(panel);

const COLS = {
  full: ["ACC_X", "ACC_Y", "ACC_Z", "GYRO_X", "GYRO_Y", "GYRO_Z", "EOG_L", "EOG_R", "EOG_H", "EOG_V"],
  standard: ["ACC_X", "ACC_Y", "ACC_Z", "EOG_L1", "EOG_R1", "EOG_L2", "EOG_R2", "EOG_H1", "EOG_H2", "EOG_V1", "EOG_V2"],
  quaternion: ["QUATERNION_W", "QUATERNION_X", "QUATERNION_Y", "QUATERNION_Z"],
};
let timer = null, i = 0, mode = "full";
const g = () => (Math.random() + Math.random() + Math.random() - 1.5) * 2;
function sample(t) {
  const walk = (t % 30) < 18, blink = Math.exp(-((((t % 3.3) - 0.2) / 0.06) ** 2)) * 300;
  const s = Math.sin(2 * Math.PI * 1.9 * t), s2 = Math.sin(2 * Math.PI * 0.95 * t);
  const v = blink + 6 * g(), h = 80 * Math.sign(Math.sin(2 * Math.PI * 0.2 * t)) + 5 * g();
  return {
    ACC_X: 4096 * (walk ? 0.08 * s : 0) + 40 * g(), ACC_Y: -4096 * 0.98 + 4096 * (walk ? 0.25 * s : 0) + 40 * g(), ACC_Z: 300 + 40 * g(),
    GYRO_X: 32.8 * (walk ? 22 * s2 : 0) + 40 * g(), GYRO_Y: 32.8 * (walk ? 11 * s : 0) + 40 * g(), GYRO_Z: 40 * g(),
    EOG_L: v / 2, EOG_R: -v / 2, EOG_H: h, EOG_V: v, EOG_L1: v / 2, EOG_R1: -v / 2, EOG_L2: v / 2, EOG_R2: -v / 2,
    EOG_H1: h, EOG_H2: h, EOG_V1: v, EOG_V2: blink * 0.9 + 6 * g(), QUATERNION_W: 1, QUATERNION_X: 0, QUATERNION_Y: 0, QUATERNION_Z: 0,
  };
}
document.getElementById("devStart").onclick = () => {
  clearInterval(timer);
  mode = document.getElementById("devMode").value; i = 0;
  const cps = 100, cols = COLS[mode], t0 = performance.now();
  const features = document.getElementById("devFeatures").checked ? ["notify", "records"] : [];
  app.begin({ startedAt: Date.now(), cps, i0: 1000, features });
  host.start(JSON.stringify({ label: `Simulated ${mode}`, mode, cps, accRange: 8, gyroRange: 1000, columns: cols, startedAt: app.startedAt, timeZone: "local", features }));
  timer = setInterval(() => {
    const due = Math.floor((performance.now() - t0) / 1000 * cps), rows = [];
    for (; i < due; i++) { const s = sample(i / cps); rows.push([i + 1000, ...cols.map((c) => Math.round(s[c]))]); }
    if (rows.length) host.push(JSON.stringify(rows));
    host.status(`${cps} Hz · battery 4`);
  }, 50);
};
document.getElementById("devStop").onclick = () => { clearInterval(timer); host.stop(); app.end(); };
document.getElementById("devGap").onclick = () => host.gap();
document.getElementById("devMark").onclick = () => host.mark(JSON.stringify({ i: i + 1000, text: "X" }));
document.getElementById("devCsv").onchange = (e) => {
  const f = e.target.files[0];
  if (f) { clearInterval(timer); app.end(); host.openReplay(JSON.stringify({ url: URL.createObjectURL(f), name: f.name })); }
};

// ---------------------------------------------------------------- アプリの受け口の真似(notify / table / records)
// Android の実装はこれと同じ規則にする(名前・列名の形、上限、文字列の扱い、ライブの間だけ受ける、同じ tag は 2 秒に 1 件)。
const NAME = /^[a-z][a-z0-9_]{0,31}$/, COLUMN = /^[A-Z][A-Z0-9_]{0,31}$/, CTRL = /[\u0000-\u001f\u007f]/, FORMULA = /^[=+\-@]/;
const MAX_MESSAGE = 64000, MAX_TAGS = 16, MAX_TABLES = 16, TAG_INTERVAL_MS = 2000;
const appBox = document.createElement("div");
appBox.style.cssText = "flex-basis:100%;font:11px/1.4 ui-monospace,monospace;white-space:pre-wrap";
panel.append(appBox);

const app = {
  live: false, features: [], startedAt: 0, cps: 100, i0: 0, page: "",
  tables: new Map(),   // name → {title, columns, lines: [CSV の行], bad: 列が違う宣言し直しを受けた}
  tags: new Map(),     // tag → {last: 最後に出した時刻, pending: 2 秒待ちの通知, timer}
  notices: [], warnings: [],
  begin({ startedAt, cps, i0, features }) {
    for (const t of this.tags.values()) clearTimeout(t.timer);
    Object.assign(this, { live: true, features, startedAt, cps, i0, tables: new Map(), tags: new Map(), notices: [], warnings: [] });
    document.getElementById("devTables").disabled = true;
    this.show();
  },
  end() {
    if (!this.live) return;
    this.live = false;
    document.getElementById("devTables").disabled = ![...this.tables.values()].some((t) => t.lines.length);
    this.show();
  },
  warn(text) { this.warnings.push(text); console.warn("[dev app]", text); this.show(); },
  /** DATE 列: アプリはデータ CSV の同じ NUM の行の DATE を書く。ここでは開始時刻と cps から作る(UTC。データ CSV と同じ書式) */
  date(i) {
    const d = new Date(this.startedAt + (i - this.i0) / this.cps * 1000), p = (n, w = 2) => String(n).padStart(w, "0");
    return `${d.getUTCFullYear()}/${p(d.getUTCMonth() + 1)}/${p(d.getUTCDate())} ${p(d.getUTCHours())}:${p(d.getUTCMinutes())}:${p(d.getUTCSeconds())}.${p(d.getUTCMilliseconds(), 3)}`;
  },
  text(v, max) { return typeof v === "string" && v.length <= max && !CTRL.test(v) ? v : null; },
  cell(v) {
    if (v === null) return "";
    if (typeof v === "number" && Number.isFinite(v)) return String(v);
    if (typeof v === "string") {
      const s = v.slice(0, 64).replace(/[,\r\n]/g, " ");
      return FORMULA.test(s) ? "" : s;
    }
    return undefined;
  },

  receive(msg) {
    if (msg.kind === "ready") { this.page = `${msg.name ?? ""} ${msg.version ?? ""}`.trim(); return; }   // CSV の前書きの Page(アプリと同じ)
    if (!["notify", "table", "records"].includes(msg.kind)) return;
    if (JSON.stringify(msg).length > MAX_MESSAGE) return this.warn(`${msg.kind}: message over ${MAX_MESSAGE} characters, ignored`);
    const need = msg.kind === "notify" ? "notify" : "records";
    if (!this.features.includes(need)) return this.warn(`${msg.kind}: the app did not offer "${need}" in features; the page should not send it`);
    if (!this.live) return this.warn(`${msg.kind}: not in a live measurement, ignored`);
    this[msg.kind](msg);
  },

  notify({ tag, title, text, i }) {
    if (typeof tag !== "string" || !NAME.test(tag)) return this.warn(`notify: bad tag ${JSON.stringify(tag)}`);
    const t = this.text(title, 64);
    if (!t) return this.warn(`notify ${tag}: title is required (at most 64 characters, no control characters)`);
    if (text != null && this.text(text, 200) == null) return this.warn(`notify ${tag}: text must be at most 200 characters without control characters`);
    if (i != null && !(Number.isInteger(i) && i >= 0)) return this.warn(`notify ${tag}: i must be a sample number`);
    if (!this.tags.has(tag) && this.tags.size >= MAX_TAGS) return this.warn(`notify: more than ${MAX_TAGS} tags, ${tag} ignored`);
    const st = this.tags.get(tag) ?? { last: -Infinity, pending: null, timer: 0 };
    this.tags.set(tag, st);
    const n = { tag, title: t, text: text ?? "", i };
    const wait = st.last + TAG_INTERVAL_MS - performance.now();
    if (wait <= 0) return this.shown(st, n);
    st.pending = n;                                       // 2 秒以内の続き: 最後の 1 件だけを後で出す
    if (!st.timer) st.timer = setTimeout(() => { st.timer = 0; if (st.pending && this.live) this.shown(st, st.pending); st.pending = null; }, wait);
  },
  shown(st, n) {
    st.last = performance.now();
    this.notices.push(n);
    console.info("[dev app] notification", n);
    this.show();
  },

  table({ name, columns, title }) {
    if (typeof name !== "string" || !NAME.test(name) || name === "disconnect") return this.warn(`table: bad name ${JSON.stringify(name)}`);
    if (!Array.isArray(columns) || columns.length < 1 || columns.length > 32 ||
        columns.some((c) => typeof c !== "string" || !COLUMN.test(c) || c === "NUM" || c === "DATE") || new Set(columns).size !== columns.length) {
      return this.warn(`table ${name}: bad columns ${JSON.stringify(columns)}`);
    }
    if (title != null && this.text(title, 64) == null) return this.warn(`table ${name}: title must be at most 64 characters without control characters`);
    const old = this.tables.get(name);
    if (old) {                                             // 宣言し直し: 同じ列なら同じファイルへ続ける
      if (old.columns.join() !== columns.join()) { old.bad = true; this.warn(`table ${name}: declared again with other columns; its rows are dropped`); }
      return;
    }
    if (this.tables.size >= MAX_TABLES) return this.warn(`table: more than ${MAX_TABLES} tables, ${name} ignored`);
    this.tables.set(name, { title: title ?? "", columns, lines: [], bad: false });
    this.show();
  },

  records({ name, rows }) {
    const t = this.tables.get(name);
    if (!t) return this.warn(`records: table ${JSON.stringify(name)} was not declared`);
    if (t.bad) return;
    if (!Array.isArray(rows)) return this.warn(`records ${name}: rows must be an array`);
    let dropped = 0;
    for (const r of rows) {
      const i = Array.isArray(r) ? r[0] : null;
      const cells = Array.isArray(r) && r.length === t.columns.length + 1 && Number.isInteger(i) && i >= 0 ? r.slice(1).map((v) => this.cell(v)) : null;
      if (!cells || cells.includes(undefined)) { dropped++; continue; }
      t.lines.push([i, this.date(i), ...cells].join(","));
    }
    if (dropped) this.warn(`records ${name}: ${dropped} row(s) dropped (need [i, ${t.columns.length} values], values: number, string or null)`);
    this.show();
  },

  /** アプリが書くのと同じ形の CSV(前書きと列名の行は //) */
  csv(name, t) {
    const base = `SIMULATED_${new Date(this.startedAt).toISOString().replace(/\D/g, "").slice(0, 14)}`;
    return [`// Detector output  : ${name}${t.title ? ` (${t.title})` : ""}`, `// Page  : ${this.page}`, `// Data file  : ${base}.csv.gz`, "//",
      "//" + ["NUM", "DATE", ...t.columns].join(","), ...t.lines].join("\r\n") + "\r\n";
  },
  download() {
    const base = `SIMULATED_${new Date(this.startedAt).toISOString().replace(/\D/g, "").slice(0, 14)}`;
    for (const [name, t] of this.tables) {
      if (!t.lines.length) continue;                       // 行が 0 の表はファイルを作らない(アプリと同じ)
      const a = document.createElement("a");
      a.href = URL.createObjectURL(new Blob([this.csv(name, t)], { type: "text/csv" }));
      a.download = `${base}_${name}.csv`;
      a.click();
      setTimeout(() => URL.revokeObjectURL(a.href), 1000);
    }
  },

  show() {
    const lines = [`App: ${this.live ? "live" : "idle"} · features [${this.features.join(", ")}]`];
    for (const [name, t] of this.tables) lines.push(`  table ${name}: ${t.lines.length} rows${t.bad ? " (columns changed, dropping)" : ""} — ${t.columns.join(", ")}`);
    lines.push(`  notifications: ${this.notices.length}`);
    for (const n of this.notices.slice(-5)) lines.push(`    [${n.tag}] ${n.title}${n.text ? " — " + n.text : ""}${n.i != null ? ` (i ${n.i})` : ""}`);
    for (const w of this.warnings.slice(-5)) lines.push(`  ! ${w}`);
    appBox.textContent = lines.join("\n");
  },
};
window.jmasDevSink = (msg) => app.receive(msg);
window.jmasDevApp = app;      // 確かめる道具(ヘッドレスのブラウザ・コンソール)から見る
document.getElementById("devTables").onclick = () => app.download();
app.show();

// ?replay=<URL> で開いた時点から再生する(serve.py --data と組み合わせる)
const replayUrl = new URLSearchParams(location.search).get("replay");
if (replayUrl) host.openReplay(JSON.stringify({ url: replayUrl, name: replayUrl.split("/").pop() }));

// ?mock=full|standard|quaternion で模擬の計測をすぐ始める
const mockMode = new URLSearchParams(location.search).get("mock");
if (mockMode) { document.getElementById("devMode").value = mockMode; document.getElementById("devStart").click(); }

// エラーはパネルにも出す(ヘッドレスのスクリーンショットで見えるように)
const errs = document.createElement("pre");
errs.style.cssText = "flex-basis:100%;margin:0;color:#c00;white-space:pre-wrap;font-size:11px";
panel.append(errs);
addEventListener("error", (e) => { errs.textContent += `${e.message} (${e.filename}:${e.lineno})\n`; });
addEventListener("unhandledrejection", (e) => { errs.textContent += `${e.reason?.stack || e.reason}\n`; });

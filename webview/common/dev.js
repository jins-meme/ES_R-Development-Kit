// 開発用(ブラウザで ?dev を付けて開いたときだけ読む): アプリの代わりに jmasHost を呼ぶ小さなパネル。
//   ・模擬の計測(Full / Standard / Quaternion、100 Hz)… アプリと同じく 0.05 秒ごとに push する
//   ・CSV を開く … ファイルを選ぶと blob URL を openReplay に渡す(アプリは仮想ホストの URL を渡す)
// アプリへの post はブラウザでは console に出るだけ。
const host = window.jmasHost;
const panel = document.createElement("div");
panel.style.cssText = "max-width:100%;box-sizing:border-box;overflow:hidden;display:flex;flex-wrap:wrap;gap:8px;align-items:center;padding:8px;border:1px dashed var(--line);border-radius:8px;margin-bottom:8px;font-size:12px";
panel.innerHTML = `<b>Dev</b>
  <select id="devMode"><option>full</option><option>standard</option><option>quaternion</option></select>
  <button id="devStart" type="button">Start simulated measurement</button>
  <button id="devStop" type="button">Stop</button>
  <button id="devGap" type="button">Gap</button>
  <button id="devMark" type="button">Free Marking</button>
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
  host.start(JSON.stringify({ label: `Simulated ${mode}`, mode, cps, accRange: 8, gyroRange: 1000, columns: cols, startedAt: Date.now(), timeZone: "local" }));
  timer = setInterval(() => {
    const due = Math.floor((performance.now() - t0) / 1000 * cps), rows = [];
    for (; i < due; i++) { const s = sample(i / cps); rows.push([i + 1000, ...cols.map((c) => Math.round(s[c]))]); }
    if (rows.length) host.push(JSON.stringify(rows));
    host.status(`${cps} Hz · battery 4`);
  }, 50);
};
document.getElementById("devStop").onclick = () => { clearInterval(timer); host.stop(); };
document.getElementById("devGap").onclick = () => host.gap();
document.getElementById("devMark").onclick = () => host.mark(JSON.stringify({ i: i + 1000, text: "X" }));
document.getElementById("devCsv").onchange = (e) => {
  const f = e.target.files[0];
  if (f) { clearInterval(timer); host.openReplay(JSON.stringify({ url: URL.createObjectURL(f), name: f.name })); }
};

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

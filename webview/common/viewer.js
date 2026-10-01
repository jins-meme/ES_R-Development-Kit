// ページの骨組み(標準版・高機能版で共通): 操作バー・グラフの束・下から出るパネル・トースト・ブリッジ。
//
//   createViewer(root, { specs(ctx) → spec[], onStart?(ctx), onRows?(ctx, rows), onReplayLoaded?(ctx),
//                        onGap?(ctx), onStop?(ctx), status?(ctx) → 文字列 })
//     specs はモード(列)が決まるたびに呼ばれ、表示するグラフを返す。ctx = { store, cond, t0 }。
//     status はステータス欄の右に足す 1 行(毎コマ呼ぶ。高機能版の検出器の進み具合など)。
//
// 入力は 2 通り:
//   ライブ … アプリが jmasHost.start → push を呼ぶ(BLE はアプリ側)。直近 30 分を持つ。
//   再生   … アプリが jmasHost.openReplay({url}) を呼び、ページが CSV を読んで再生を受け持つ(全長を持つ)。
// 仕様: python-processing-core docs/porting-html/devkit-webview.md(§3・§4・§5)、ブリッジは webview/BRIDGE.md。
import { installHost, post, embedded, BRIDGE_API } from "./bridge.js";
import { RowStore } from "./store.js";
import { CHART_COLUMNS, readCsv } from "./csv.js";
import { View } from "./view.js";
import { ChartStack } from "./charts.js";

const LIVE_HIST_SEC = 1800;                    // ライブで巻き戻せる長さ [s]
const WINS = [60, 30, 15, 10];
const SPEEDS = [1, 2, 4, 8, 16, 32];           // 再生速度(今の Mac 版と同じ)
const COMPACT = "(pointer: coarse), (max-width: 640px)";
/** アーティファクトの文字で拒否する書き出し(表計算ソフトの数式。webview/BRIDGE.md の artifact) */
export const FORMULA_START = /^[=+\-@\t\r]/;

const el = (tag, cls, text) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };
const button = (label, fn, cls, title) => { const b = el("button", cls, label); b.type = "button"; if (title) b.title = title; b.addEventListener("click", fn); return b; };

export function createViewer(root, hooks) {
  const compact = matchMedia(COMPACT).matches;
  document.documentElement.classList.toggle("compact", compact);
  const view = new View({ defWin: compact ? 10 : 30, maxWin: compact ? 60 : 120 });

  // ---------------------------------------------------------------- 画面の部品
  root.classList.add("viewer");
  const bar = el("div", "bar");
  // 表示幅: PC は「Window」+ 4 つのボタン + 4 つ以外の幅の表示(元のまま)、スマホは幅が足りないのでドロップダウン
  // (ピンチで 4 つ以外の幅になったら、その幅を末尾の項目に出して選んだ状態にする)。レイアウトの指示は PC とスマホで別(本人)
  const winLabel = el("span", "readout", "Window");
  const winBtns = el("div", "group"); winBtns.setAttribute("role", "group"); winBtns.setAttribute("aria-label", "Window");
  for (const w of WINS) winBtns.append(button(`${w}s`, () => view.setWin(w), "", `Show ${w} seconds`));
  const winReadout = el("span", "readout");
  const winSel = el("select", "speed"); winSel.id = "winSelect"; winSel.title = "Window"; winSel.setAttribute("aria-label", "Window");
  for (const w of WINS) { const o = el("option", "", `${w} s`); o.value = String(w); winSel.append(o); }
  const winOther = el("option", ""); winOther.value = "other"; winOther.disabled = true; winOther.hidden = true; winSel.append(winOther);
  winSel.addEventListener("change", () => { const w = Number(winSel.value); if (w) view.setWin(w); });
  const winControls = compact ? [winSel] : [winLabel, winBtns, winReadout];
  const nav = el("div", "group");
  const rew = button("◀◀", () => view.setRight(view.right - view.win / 2), "", "Back half a window");
  const play = button("⏸", () => togglePlay(), "", "Play / pause");
  const fwd = button("▶▶", () => view.setRight(view.right + view.win / 2), "", "Forward half a window");
  nav.append(rew, play, fwd);
  const speed = el("select", "speed"); speed.id = "speedSelect"; speed.title = "Playback speed"; speed.setAttribute("aria-label", "Playback speed");
  SPEEDS.forEach((v, k) => { const o = el("option", "", `x${v}`); o.value = String(k); speed.append(o); });
  speed.addEventListener("change", () => { if (ctx.replay) ctx.replay.speed = Number(speed.value); });
  const live = button("LIVE", () => view.goLive(), "live", "Show the latest");
  const resetAll = button("Reset view", () => { view.reset(); charts.resetAllY(); }, "reset", "Reset time and vertical zoom");
  bar.append(...winControls, el("span", "spacer"), nav, speed, live, resetAll);

  const scrub = el("div", "scrub");
  const range = el("input"); range.type = "range"; range.min = "0"; range.max = "1000"; range.step = "1"; range.id = "scrubRange";
  range.setAttribute("aria-label", "Playback position");
  const pos = el("span", "readout");
  scrub.append(range, pos);

  const status = el("div", "status");
  // 左はファイル名・端末名。アプリの中ではアプリ側に出ているので出さない(ブラウザで開いたときだけ)
  const statusL = el("span", "src"), statusR = el("span", "info");
  statusL.hidden = embedded();
  status.append(statusL, statusR);
  const message = el("div", "message");
  const chartsEl = el("div", "charts");
  const sheet = el("div", "sheet"); sheet.hidden = true;
  const toast = el("div", "toast"); toast.hidden = true; toast.setAttribute("role", "status");
  root.append(bar, scrub, status, message, chartsEl, sheet, toast);
  play.hidden = speed.hidden = scrub.hidden = live.hidden = resetAll.hidden = true;   // 最初のコマまで出さない

  // ---------------------------------------------------------------- 状態
  const ctx = { store: null, cond: null, t0: null, mode: "idle", replay: null };
  let hostStatus = "";
  const charts = new ChartStack(chartsEl, view, {
    compact,
    fmtTime: (t, span) => fmtTime(t, span),
    t0: () => ctx.t0 ?? 0,
    onTap: (t) => openArtifactSheet(t),
    onOpenY: (c) => openYSheet(c.spec),
    layout: hooks.chartLayout,
  });

  function fmtTime(t, span) {
    if (ctx.t0 == null) {
      const s = Math.max(0, t), m = Math.floor(s / 60), r = s - m * 60;
      return `${String(m).padStart(2, "0")}:${span < 8 ? r.toFixed(1).padStart(4, "0") : String(Math.floor(r)).padStart(2, "0")}`;
    }
    const d = new Date(ctx.t0 + t * 1000), utc = ctx.cond?.timeZone === "utc";
    const H = utc ? d.getUTCHours() : d.getHours(), M = utc ? d.getUTCMinutes() : d.getMinutes();
    const S = (utc ? d.getUTCSeconds() : d.getSeconds()) + (utc ? d.getUTCMilliseconds() : d.getMilliseconds()) / 1000;
    const p = (v) => String(v).padStart(2, "0");
    return `${p(H)}:${p(M)}:${span < 8 ? S.toFixed(1).padStart(4, "0") : p(Math.floor(S))}`;
  }

  function showMessage(text) { message.textContent = text; message.hidden = !text; }
  function showToast(text, ms = 3000) {
    toast.textContent = text; toast.hidden = false;
    clearTimeout(showToast.timer); showToast.timer = setTimeout(() => { toast.hidden = true; }, ms);
  }

  /** 列が決まったら(start / 再生の読み込み)グラフを組み直す */
  function setup(store, cond, t0) {
    ctx.store = store; ctx.cond = cond; ctx.t0 = t0;
    charts.artifacts = []; charts.gaps = [];
    charts.resetAllY();                                     // 縦の拡大率は次の計測に引き継がない
    view.reset();
    view.oldest = () => store.first / store.cps;
    const specs = hooks.specs(ctx).filter((s) => s.data(0, 0) !== null);
    charts.build(specs);
    showMessage(specs.length ? "" : "No charts for this mode");
    hooks.onStart?.(ctx);
  }

  // ---------------------------------------------------------------- ライブ(アプリから)
  let colMap = null;
  function startLive(cond) {
    stopReplay();
    ctx.mode = "live";
    applyTheme(cond.theme);
    const columns = cond.columns ?? [];
    const cols = CHART_COLUMNS.filter((c) => columns.includes(c));
    colMap = cols.map((c) => 1 + columns.indexOf(c));        // rows の各行は [i, 値…]
    const store = new RowStore(cols, cond.cps ?? 100, { capRows: LIVE_HIST_SEC * (cond.cps ?? 100) });
    store.meta = cond;
    view.head = () => Math.max(0, (store.n - 1) / store.cps);
    view.seek = null;
    setup(store, cond, cond.startedAt ?? null);
    statusL.textContent = cond.label ?? "Measuring";
    hostStatus = "";
  }
  function pushRows(rows) {
    const store = ctx.store;
    if (!store || ctx.mode !== "live") return;
    const vals = new Array(colMap.length);
    for (const r of rows) {
      for (let k = 0; k < colMap.length; k++) vals[k] = r[colMap[k]];
      store.push(vals, r[0]);
    }
    hooks.onRows?.(ctx, rows);
  }

  // ---------------------------------------------------------------- 再生(ページが受け持つ)
  async function startReplay({ url, name }) {
    stopReplay();
    ctx.mode = "loading";
    statusL.textContent = name ?? url;
    showMessage("Loading…");
    charts.build([]);
    try {
      const res = await fetch(url);
      if (!res.ok) throw new Error(`Could not read ${name ?? url} (HTTP ${res.status})`);
      const total = Number(res.headers.get("Content-Length")) || 0;
      const r = await readCsv(res.body, {
        total, onProgress: (read, tot, rows) => showMessage(`Loading… ${tot ? Math.round(read / tot * 100) + " %" : (read / 1e6).toFixed(0) + " MB"} (${(rows / 100 / 60).toFixed(0)} min)`),
      });
      const cond = { mode: r.mode, cps: r.cps, accRange: r.accRange, gyroRange: r.gyroRange, columns: r.columns,
                     timeZone: ctx.cond?.timeZone ?? ctx.pendingTimeZone ?? "local", accOffset: ctx.pendingAccOffset ?? [0, 0, 0] };
      const rp = { playing: true, speed: 0, head: 0, dur: Math.max(0, (r.store.n - 1) / r.cps), name };
      ctx.replay = rp; ctx.mode = "replay";
      view.head = () => rp.head;
      view.seek = (t) => { rp.head = Math.max(0, Math.min(rp.dur, t)); };
      setup(r.store, cond, r.startedAt);
      charts.artifacts = r.artifacts.map((a) => ({ t: a.row / r.cps, text: a.text }));
      rp.head = Math.min(rp.dur, view.win);                 // 最初の表示幅を満たした状態から始める
      post("replay-info", { mode: r.mode, cps: r.cps, accRange: r.accRange, gyroRange: r.gyroRange, rows: r.store.n,
                            startedAt: r.startedAt, warning: r.warning });
      if (r.warning) showToast(r.warning, 6000);
      hooks.onReplayLoaded?.(ctx);
    } catch (e) {
      ctx.mode = "idle";
      showMessage(String(e.message ?? e));
      post("log", { level: "error", message: `replay: ${e?.stack ?? e}` });
    }
  }
  function stopReplay() { ctx.replay = null; view.seek = null; }
  function togglePlay() { const rp = ctx.replay; if (!rp) return; if (!rp.playing && rp.head >= rp.dur) rp.head = Math.min(rp.dur, view.win); rp.playing = !rp.playing; }
  range.addEventListener("input", () => { const rp = ctx.replay; if (rp) rp.head = Number(range.value) / 1000 * rp.dur; });

  // ---------------------------------------------------------------- アーティファクト・縦のパネル
  let sheetFor = null;
  function closeSheet() { sheet.hidden = true; sheet.textContent = ""; sheetFor = null; }
  function rowAt(t) { const s = ctx.store; return Math.max(s.first, Math.min(s.n - 1, Math.round(t * s.cps))); }
  function openArtifactSheet(t) {
    const s = ctx.store;
    if (!s || !(ctx.mode === "live" || ctx.mode === "replay") || s.n === 0) return;
    const row = rowAt(t), at = row / s.cps;
    const i = ctx.mode === "live" ? s.iOfRow(row) : row;   // ライブはアプリのサンプル番号、再生は CSV のデータ行の番号
    sheet.textContent = ""; sheetFor = { kind: "artifact" };
    const input = el("input"); input.id = "artifactText"; input.placeholder = "Leave empty for X"; input.autocomplete = "off"; input.maxLength = 64;
    const note = el("div", "sub warn"); note.hidden = true;
    const ok = () => {
      const text = input.value.replace(/[,\r\n]/g, " ").trim() || "X";
      // 表計算ソフトで CSV を開いたときに数式として読まれる書き出しは受け付けない(CSV 注入。アプリ側も同じ規則で断る)
      if (FORMULA_START.test(text)) {
        note.textContent = "Cannot start with = + - @ (a spreadsheet would read it as a formula)";
        note.hidden = false; input.focus({ preventScroll: true });
        return;
      }
      charts.artifacts.push({ t: at, text });
      post("artifact", { i, text });
      closeSheet();
    };
    input.addEventListener("keydown", (e) => { if (e.key === "Enter") ok(); if (e.key === "Escape") closeSheet(); });
    const row1 = el("div", "row"); row1.append(input, button("Add", ok, "primary"), button("Cancel", closeSheet));
    input.addEventListener("input", () => { note.hidden = true; });
    sheet.append(el("h3", "", "Add artifact"), el("div", "sub", `${fmtTime(at, 1)} (${ctx.mode === "live" ? "sample" : "row"} ${i})`), row1, note);
    sheet.hidden = false;
    if (!compact) input.focus({ preventScroll: true });
  }
  function openYSheet(spec) {
    sheet.textContent = ""; sheetFor = { kind: "y", spec };
    const sub = el("div", "sub"); sub.id = "ySheetSub";
    const row1 = el("div", "row");
    row1.append(button("− Zoom out", () => charts.zoomY(spec, 2)), button("+ Zoom in", () => charts.zoomY(spec, 0.5)),
                button("Auto", () => charts.toggleAuto(spec)), button("Reset", () => charts.resetY(spec)), button("Close", closeSheet));
    sheet.append(el("h3", "", `${spec.title} — vertical`), sub, row1);
    sheet.hidden = false;
  }

  // ---------------------------------------------------------------- キー(PC)
  addEventListener("keydown", (e) => {
    if (e.target.closest?.("input, textarea, select") || e.metaKey || e.ctrlKey || e.altKey) return;
    const k = e.key;
    if (k === "+" || k === "=") view.setWin(view.win / 1.5);
    else if (k === "-") view.setWin(view.win * 1.5);
    else if (k === "0") { view.reset(); charts.resetAllY(); }
    else if (k === "ArrowLeft") view.setRight(view.right - view.win / 2);
    else if (k === "ArrowRight") view.setRight(view.right + view.win / 2);
    else if (k === " " && ctx.replay) togglePlay();
    else if (k === "Escape") closeSheet();
    else return;
    e.preventDefault();
  });

  // ---------------------------------------------------------------- テーマ
  function applyTheme(t) {
    if (t === "light" || t === "dark") document.documentElement.dataset.theme = t;
  }

  // ---------------------------------------------------------------- 毎コマ
  let lastFrame = 0, lastT = performance.now();
  const minFrameMs = compact ? 1000 / 30 - 2 : 0;
  function frame(now) {
    requestAnimationFrame(frame);
    const dt = (now - lastT) / 1000; lastT = now;
    view.tick(dt);
    const rp = ctx.replay;
    if (rp && rp.playing) {
      rp.head = Math.min(rp.dur, rp.head + dt * SPEEDS[rp.speed]);
      if (rp.head >= rp.dur) rp.playing = false;
    }
    if (now - lastFrame < minFrameMs) return;
    lastFrame = now;
    if (ctx.store) charts.render();
    // 操作バー
    const wNow = WINS.find((x) => Math.abs(x - view.win) < 0.01), wText = `${view.win.toFixed(view.win < 10 ? 1 : 0)} s`;
    if (compact) {
      if (document.activeElement !== winSel) {
        winOther.hidden = wNow != null;
        if (wNow == null) winOther.textContent = wText;
        const v = wNow != null ? String(wNow) : "other";
        if (winSel.value !== v) winSel.value = v;
      }
    } else {
      for (const b of winBtns.children) b.setAttribute("aria-pressed", String(Math.abs(parseFloat(b.textContent) - view.win) < 0.01));
      winReadout.textContent = wNow != null ? "" : wText;
    }
    const isReplay = ctx.mode === "replay" && !!rp;
    play.hidden = speed.hidden = scrub.hidden = !isReplay;
    live.hidden = isReplay || ctx.mode !== "live";
    if (isReplay) {
      play.textContent = rp.playing ? "⏸" : "▶";
      if (document.activeElement !== speed) speed.value = String(rp.speed);
      if (document.activeElement !== range) range.value = String(rp.dur ? Math.round(rp.head / rp.dur * 1000) : 0);
      pos.textContent = `${fmtTime(rp.head, 60)} / ${fmtTime(rp.dur, 60)}`;
    }
    live.className = "live " + (view.live ? "on" : "off");
    live.textContent = view.live ? "LIVE" : "Back to LIVE";
    resetAll.hidden = !(view.modified || charts.anyModified);
    const s = ctx.store;
    statusR.textContent = s ? [modeLabel(ctx.cond), hooks.status?.(ctx), hostStatus].filter(Boolean).join(" · ") : "";
    if (sheetFor?.kind === "y") {
      const sub = sheet.querySelector("#ySheetSub"), sp = sheetFor.spec, y = charts.y(sp);
      if (sub) sub.textContent = `${y.min.toFixed(sp.dp)} to ${y.max.toFixed(sp.dp)} ${sp.unit} (${charts.yModified(sp) ? charts.yLabel(sp) : "default"})`;
    }
  }
  requestAnimationFrame(frame);

  function modeLabel(c) {
    if (!c) return "";
    const m = { full: "Full", standard: "Standard", quaternion: "Quaternion" }[c.mode] ?? c.mode;
    return `${m} · ${c.cps} Hz · ±${c.accRange} g · ±${c.gyroRange} dps`;
  }

  // ---------------------------------------------------------------- ブリッジ
  installHost({
    start: (cond) => startLive(cond),
    rows: (rows) => pushRows(rows),
    gap: () => { if (ctx.store) { charts.gaps.push(ctx.store.seconds); hooks.onGap?.(ctx); } },
    status: (text) => { hostStatus = text; },
    replay: (arg) => { ctx.pendingTimeZone = arg.timeZone; ctx.pendingAccOffset = arg.accOffset; applyTheme(arg.theme); startReplay(arg); },
    mark: ({ i, text }) => { const s = ctx.store; if (s) charts.artifacts.push({ t: s.rowOfI(i) / s.cps, text: String(text) }); },
    theme: (t) => applyTheme(t),
    stop: () => { if (ctx.mode === "live" || ctx.mode === "replay") { ctx.mode = "stopped"; hostStatus = "Stopped"; } stopReplay(); hooks.onStop?.(ctx); },
  });
  showMessage(embedded() ? "" : "Start a measurement or a replay in the app to see the graphs here");
  fetch("manifest.json").then((r) => r.json()).catch(() => ({})).then((m) => {
    post("ready", { bridgeApi: BRIDGE_API, name: m.name ?? "unknown", version: m.version ?? "0" });
  });

  return { ctx, view, charts, showToast, showMessage };
}

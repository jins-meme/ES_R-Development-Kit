// グラフの束。時間(横)は全グラフ共通(View)、振幅(縦)はグラフごと。
// 操作は python-processing-core docs/porting-html/devkit-webview.md §3-5(試作 zoom-ui-proposal.html)と同じ:
//   PC   : Ctrl(⌘)+ホイール / トラックパッドのピンチ = 時間の拡大・縮小、ドラッグ・Shift+ホイール・横スワイプ = 時間の移動、
//          縦軸の帯の上でホイール = 縦の拡大・縮小、縦軸をドラッグ = 縦の移動。修飾キーなしのホイールはページのスクロール。
//   スマホ: 2 本指を横に = 時間、縦に = そのグラフの縦。1 本指の横ドラッグ = 時間の移動、縦ドラッグ = ページのスクロール。
//   タップ(クリック)はアーティファクト専用。ダブルタップは使わない。
//
// spec(1 枚ぶん): { key, title, unit, def: [min, max], dp, series: [{ name, color, scale?, width? }], data(t0, t1) → [x, y1, …] | null,
//                    draw?(u, ctx) … 追加で描くもの(ctx は uPlot の canvas)
//                    auto?   … true なら縦を「自動」で始める(Reset でも自動へ戻る)
//                    height? … [PC, スマホ] の高さ [px](既定 [170, 140])
//                    yTicks? … false なら縦軸の数字を出さない(帯だけのグラフ)
//                    scales? … 縦の拡大・縮小を受けない追加のスケール { 名前: { range: [min, max] | (data) → [min, max], distr? } }。
//                              series の scale に名前を書いた系列はそちらで描く(右の軸は出さない。凡例で示す) }
// data が null を返すグラフ(その列が無いモード)は出さない。
import uPlot from "./vendor/uPlot.esm.js";

// 歯車(Material Symbols の settings と同じ形。24 の座標系)
const GEAR_SVG = '<svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true" fill="currentColor"><path d="M19.14 12.94c.04-.3.06-.61.06-.94 ' +
  '0-.32-.02-.64-.07-.94l2.03-1.58a.49.49 0 0 0 .12-.61l-1.92-3.32a.49.49 0 0 0-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54a.48.48 0 0 0-.48-.41h-3.84' +
  'a.48.48 0 0 0-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96a.48.48 0 0 0-.59.22L2.74 8.87a.47.47 0 0 0 .12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94' +
  'l-2.03 1.58a.49.49 0 0 0-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54' +
  'c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32a.47.47 0 0 0-.12-.61l-2.01-1.58zM12 15.6A3.6 3.6 0 1 1 12 8.4a3.6 3.6 0 0 1 0 7.2z"/></svg>';

const tok = (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim();
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const el = (tag, cls, text) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };
const button = (label, fn, cls, title) => {
  const b = el("button", cls, label); b.type = "button"; if (title) b.title = title;
  b.addEventListener("click", fn); return b;
};

export class ChartStack {
  /**
   * root: グラフを入れる要素。view: View。opts:
   *   compact      スマホ表示(グラフを低く、縦の操作はパネルで)
   *   fmtTime(t, span) 横軸の目盛り
   *   onTap(t)     動かさずに離した(アーティファクト)
   *   onOpenY(chart) 縦の操作パネルを開く(スマホの「↕ 縦」・縦軸のタップ)
   */
  constructor(root, view, opts) {
    this.root = root; this.view = view; this.opts = opts;
    this.charts = [];
    this.yState = new Map();       // key → { min, max, auto }
    this.collapsed = new Set();    // 畳んだグラフの key
    this.artifacts = [];           // [{ t, text }]
    this.gaps = [];                // [t]
    this.specs = [];
    new ResizeObserver(() => this._resize()).observe(root);
    const rebuild = () => this.build(this.specs);
    matchMedia("(prefers-color-scheme: dark)").addEventListener("change", rebuild);
    new MutationObserver(rebuild).observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });
  }

  // ---------------------------------------------------------------- 縦
  y(spec) {
    if (!this.yState.has(spec.key)) this.resetY(spec);
    return this.yState.get(spec.key);
  }
  resetY(spec) { this.yState.set(spec.key, { min: spec.def[0], max: spec.def[1], auto: !!spec.auto }); }
  resetAllY() { for (const s of this.specs) this.resetY(s); }
  zoomY(spec, factor, anchor = null) {
    const y = this.y(spec), def = spec.def[1] - spec.def[0];
    const s = clamp((y.max - y.min) * factor, def / 64, def * 8);
    const a = anchor ?? (y.min + y.max) / 2, f = (a - y.min) / (y.max - y.min);
    y.min = a - f * s; y.max = y.min + s; y.auto = false;
  }
  panY(spec, dv) { const y = this.y(spec); y.min += dv; y.max += dv; y.auto = false; }
  toggleAuto(spec) { const y = this.y(spec); y.auto = !y.auto; }
  yModified(spec) {
    const y = this.y(spec), def = spec.def[1] - spec.def[0];
    if (y.auto !== !!spec.auto) return true;
    return !y.auto && (Math.abs(y.min - spec.def[0]) > def * 0.005 || Math.abs(y.max - spec.def[1]) > def * 0.005);
  }
  yLabel(spec) {
    const y = this.y(spec);
    if (y.auto) return "Auto";
    const r = (spec.def[1] - spec.def[0]) / (y.max - y.min);
    if (Math.abs(r - 1) < 0.01) return "Moved";
    return "×" + (r >= 10 ? r.toFixed(0) : r >= 1 ? r.toFixed(1) : r.toFixed(2));
  }
  get anyModified() { return this.specs.some((s) => this.yModified(s)); }

  // ---------------------------------------------------------------- 組む
  build(specs) {
    for (const c of this.charts) c.u.destroy();
    this.charts = []; this.root.textContent = ""; this.specs = specs;
    for (const spec of specs) {
      const H = (spec.height ?? [170, 180])[this.opts.compact ? 1 : 0];
      const c = { spec };
      const card = el("section", "chart"); c.card = card;
      const head = el("div", "chead");
      const chip = button("", () => { this.resetY(spec); }, "chip", "Reset vertical zoom of this chart"); chip.hidden = true;
      const legend = el("span", "legend");
      // スマホ表示では見出しを 1 行に収めるため、凡例は残りの幅だけ見せる。見切れていればタップで全部を折り返して見せる(もう一度で戻す)
      legend.addEventListener("click", () => {
        if (legend.classList.contains("open") || legend.scrollWidth > legend.clientWidth + 1) legend.classList.toggle("open");
        this._markClipped();
      });
      for (const s of spec.series) { const k = el("span"); const i = el("i"); i.style.background = tok(s.color); k.append(i, s.name); legend.append(k); }
      const val = el("span", "val");
      const tools = el("span", "ytools");
      const auto = button("Auto", () => this.toggleAuto(spec), "", "Keep fitting to the visible waveform");
      tools.append(button("−", () => this.zoomY(spec, 2), "icon", "Zoom out vertically"), button("+", () => this.zoomY(spec, 0.5), "icon", "Zoom in vertically"),
                   auto, button("↺", () => this.resetY(spec), "icon", "Reset vertical zoom of this chart"));
      // スマホ表示でだけ出す「このグラフの設定」ボタン(縦の拡大・縮小のパネルを開く)。アイコンは歯車(本人指示)。
      // 文字の ⚙ は Android で色付きの絵文字になることがあるので SVG で描く(色は文字色に合わせる)
      const yopen = button("", () => this.opts.onOpenY?.(c), "yopen icon", "Chart settings");
      yopen.setAttribute("aria-label", "Chart settings");
      yopen.innerHTML = GEAR_SVG;
      const fold = button("", () => this.toggleFold(c), "fold icon");
      head.append(fold, el("h2", "ctitle", spec.title), el("span", "unit", spec.unit), chip, legend, val, el("span", "spacer"), tools, yopen);
      const plot = el("div", "plot");
      card.append(head, plot); this.root.append(card);
      Object.assign(c, { chip, val, fold, auto, plot, tools, yopen });
      card.classList.toggle("collapsed", this.collapsed.has(spec.key));
      this._foldLabel(fold, this.collapsed.has(spec.key));

      const muted = tok("--muted"), grid = tok("--grid");
      const yw = this.opts.compact ? 44 : 56;          // 縦軸の帯の幅(スマホは狭くしてグラフを広く)
      const axis = { stroke: muted, font: "11px ui-monospace, SFMono-Regular, Menlo, Consolas, monospace",
                     grid: { stroke: grid, width: 1 }, ticks: { stroke: grid, width: 1, size: 4 } };
      c.u = new uPlot({
        width: Math.max(200, plot.clientWidth || this.root.clientWidth - 16), height: H,
        legend: { show: false }, pxAlign: 0,
        cursor: { y: false, points: { show: false }, drag: { x: false, y: false, setScale: false } },
        scales: { x: { time: false, auto: false }, y: { auto: false },
                  ...Object.fromEntries(Object.entries(spec.scales ?? {}).map(([k, s]) => [k, { auto: false, distr: s.distr ?? 1 }])) },
        axes: [
          // 目盛りの間隔は時刻の文字の幅に合わせる(既定の間隔だと HH:mm:ss が詰まって重なる)
          { ...axis, incrs: [0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600],
            // 目盛りは時計の切りのよいところ(:15、:20 …)に置く。横軸の値は計測開始からの秒なので、開始時刻のずれを足して揃える
            splits: (u, ai, min, max, incr) => {
              const o = (this.opts.t0?.() ?? 0) / 1000 % 3600, out = [];
              for (let v = Math.ceil((min + o) / incr - 1e-9) * incr - o; v <= max + 1e-9; v += incr) out.push(v);
              return out;
            },
            space: (u) => { const c = u.ctx; c.save(); c.font = axis.font; const w = c.measureText(this.opts.fmtTime(0, this.view.win)).width; c.restore(); return w + 28; },
            values: (u, vals) => vals.map((v) => this.opts.fmtTime(v, this.view.win)) },
          spec.yTicks === false
            ? { ...axis, size: yw, values: () => [], grid: { show: false }, ticks: { show: false } }
            : { ...axis, size: yw, space: 28, values: (u, vals) => vals.map((v) => (Math.abs(v) < 1e-9 ? "0" : v.toFixed(spec.dp))) },
        ],
        series: [{}, ...spec.series.map((s) => ({ label: s.name, stroke: tok(s.color), width: s.width ?? 1.25, scale: s.scale ?? "y", points: { show: false } }))],
        hooks: {
          draw: [(u) => { this._drawMarks(u, c === this.charts[0] || this.charts.length === 0); spec.draw?.(u, u.ctx); }],
          setCursor: [(u) => {
            const i = u.cursor.idx;
            // カーソル位置の値。データのある系列だけ(凡例だけの項目 = width 0 や空の列は出さない)。グラフの右上に重ねて出す(ui.css)
            c.val.textContent = i == null ? "" : spec.series.map((s, j) => {
              const v = u.data[j + 1]?.[i];
              return s.width === 0 || v == null || Number.isNaN(v) ? null : `${s.name} ${v.toFixed(spec.dp)}`;
            }).filter(Boolean).join(" · ");
          }],
          setSize: [() => this._placeGutter(c)],
        },
      }, [[], ...spec.series.map(() => [])], plot);
      c.gutter = el("div", "gutter"); c.gutter.title = "Wheel: zoom vertically · Drag: move up / down";
      c.u.root.querySelector(".u-wrap").append(c.gutter);
      this._placeGutter(c);
      this._wirePlot(c); this._wireGutter(c);
      this.charts.push(c);
    }
    requestAnimationFrame(() => this._markClipped());
  }

  /** 見切れている凡例に印(右端をぼかす。タップで開ける合図) */
  _markClipped() {
    for (const c of this.charts) {
      const l = c.card.querySelector(".legend");
      if (l) l.classList.toggle("clipped", !l.classList.contains("open") && l.scrollWidth > l.clientWidth + 1);
    }
  }

  toggleFold(c) {
    const k = c.spec.key;
    this.collapsed.has(k) ? this.collapsed.delete(k) : this.collapsed.add(k);
    const folded = this.collapsed.has(k);
    c.card.classList.toggle("collapsed", folded);
    this._foldLabel(c.fold, folded);
    if (!folded) { c.u.setSize({ width: Math.max(200, c.plot.clientWidth), height: c.u.height }); }
  }

  _foldLabel(b, folded) {
    b.textContent = "";                 // 文字は出さない(▾ / ▸ は CSS)
    b.title = folded ? "Expand" : "Collapse";
    b.setAttribute("aria-label", b.title);
    b.setAttribute("aria-expanded", String(!folded));
  }

  _resize() {
    this._markClipped();
    for (const c of this.charts) {
      if (this.collapsed.has(c.spec.key)) continue;
      const w = Math.max(200, c.plot.clientWidth);
      if (Math.abs(w - c.u.width) > 1) c.u.setSize({ width: w, height: c.u.height });
    }
  }
  _placeGutter(c) {
    const b = c.u.bbox, px = uPlot.pxRatio;
    Object.assign(c.gutter.style, { left: "0px", top: b.top / px + "px", width: b.left / px + "px", height: b.height / px + "px" });
  }

  // ---------------------------------------------------------------- 描く(毎コマ)
  render() {
    const r = this.view.right, l = r - this.view.win;
    for (const c of this.charts) {
      const s = c.spec;
      c.chip.hidden = !this.yModified(s);
      if (!c.chip.hidden) c.chip.textContent = `${this.yLabel(s)} ↺`;
      c.auto.setAttribute("aria-pressed", String(this.y(s).auto));
      if (this.collapsed.has(s.key)) continue;
      const data = s.data(l, r) ?? [[], ...s.series.map(() => [])];
      const y = this.y(s);
      if (y.auto) this._autoFit(s, data);
      c.u.batch(() => {
        c.u.setData(data, false);
        c.u.setScale("x", { min: l, max: r });
        c.u.setScale("y", { min: y.min, max: y.max });
        for (const [k, sc] of Object.entries(s.scales ?? {})) {
          const [min, max] = typeof sc.range === "function" ? sc.range(data) : sc.range;
          c.u.setScale(k, { min, max });
        }
      });
    }
  }
  _autoFit(spec, data) {
    let lo = Infinity, hi = -Infinity;
    for (let j = 1; j < data.length; j++) {
      if ((spec.series[j - 1]?.scale ?? "y") !== "y") continue;           // 追加のスケールの系列は縦の自動に入れない
      const a = data[j]; for (let i = 0; i < a.length; i++) { const v = a[i]; if (v < lo) lo = v; if (v > hi) hi = v; }
    }
    if (!isFinite(lo)) return;
    const def = spec.def[1] - spec.def[0], span = Math.max(hi - lo, def / 64) * 1.15, mid = (lo + hi) / 2;
    const y = this.y(spec); y.min = mid - span / 2; y.max = mid + span / 2;
  }
  _drawMarks(u, withText) {
    const ctx = u.ctx, px = uPlot.pxRatio, { left, top, width, height } = u.bbox;
    const xmin = u.scales.x.min, xmax = u.scales.x.max;
    if (xmin == null) return;
    ctx.save();
    ctx.lineWidth = 1 * px; ctx.setLineDash([4 * px, 4 * px]); ctx.strokeStyle = tok("--faint");
    for (const t of this.gaps) {
      if (t < xmin || t > xmax) continue;
      const x = Math.round(u.valToPos(t, "x", true)) + 0.5;
      ctx.beginPath(); ctx.moveTo(x, top); ctx.lineTo(x, top + height); ctx.stroke();
    }
    ctx.setLineDash([]);
    ctx.strokeStyle = tok("--artifact"); ctx.fillStyle = tok("--artifact"); ctx.lineWidth = 1.5 * px;
    ctx.font = `600 ${11 * px}px system-ui, sans-serif`; ctx.textBaseline = "top";
    // 文字は近い印どうしで重ならないよう、段をずらす(最大 3 段)
    const ends = [-Infinity, -Infinity, -Infinity];
    const shown = this.artifacts.filter((a) => a.t >= xmin && a.t <= xmax).sort((a, b) => a.t - b.t);
    for (const a of shown) {
      const x = Math.round(u.valToPos(a.t, "x", true)) + 0.5;
      ctx.beginPath(); ctx.moveTo(x, top); ctx.lineTo(x, top + height); ctx.stroke();
      if (!withText) continue;
      const w = ctx.measureText(a.text).width, tx = Math.min(x + 4 * px, left + width - w - 2 * px);
      let lane = ends.findIndex((e) => tx > e + 6 * px);
      if (lane < 0) lane = ends.indexOf(Math.min(...ends));
      ctx.fillText(a.text, tx, top + (3 + lane * 13) * px);
      ends[lane] = tx + w;
    }
    ctx.restore();
  }

  // ---------------------------------------------------------------- 操作
  _wirePlot(c) {
    const over = c.u.over, pts = new Map(), view = this.view, spec = c.spec;
    let g = null;
    const plotW = () => c.u.bbox.width / uPlot.pxRatio;
    const tAt = (x) => c.u.posToVal(x, "x");
    const yAt = (y) => c.u.posToVal(y, "y");
    over.addEventListener("pointerdown", (e) => {
      over.setPointerCapture(e.pointerId);
      pts.set(e.pointerId, { x: e.offsetX, y: e.offsetY });
      if (pts.size === 1) {
        g = { type: "maybe", x0: e.offsetX, y0: e.offsetY, t0: performance.now(), r0: view.right, touch: e.pointerType === "touch" };
      } else if (pts.size === 2) {
        const [a, b] = [...pts.values()];
        const dx = Math.abs(a.x - b.x), dy = Math.abs(a.y - b.y), axis = dy > dx * 1.2 ? "y" : "x";
        const y = this.y(spec);
        g = { type: "pinch", axis, d0: Math.max(24, axis === "x" ? dx : dy), win0: view.win, y0: { min: y.min, max: y.max },
              midT: tAt((a.x + b.x) / 2), midV: yAt((a.y + b.y) / 2) };
      }
    });
    over.addEventListener("pointermove", (e) => {
      if (!pts.has(e.pointerId)) return;
      pts.set(e.pointerId, { x: e.offsetX, y: e.offsetY });
      if (!g) return;
      if (g.type === "pinch" && pts.size === 2) {
        const [a, b] = [...pts.values()];
        const d = Math.max(24, g.axis === "x" ? Math.abs(a.x - b.x) : Math.abs(a.y - b.y));
        if (g.axis === "x") {
          if (view.live) view.setWin(g.win0 * g.d0 / d);
          else { view.win = view.clampWin(g.win0 * g.d0 / d); const f = ((a.x + b.x) / 2) / plotW(); view.setRight(g.midT - f * view.win + view.win); }
        } else {
          const def = spec.def[1] - spec.def[0];
          const span = clamp((g.y0.max - g.y0.min) * g.d0 / d, def / 64, def * 8), f = (g.midV - g.y0.min) / (g.y0.max - g.y0.min);
          const y = this.y(spec); y.min = g.midV - f * span; y.max = y.min + span; y.auto = false;
        }
        return;
      }
      const dx = e.offsetX - g.x0, dy = e.offsetY - g.y0;
      if (g.type === "maybe" && (Math.abs(dx) > 6 || Math.abs(dy) > 6)) {
        g.type = g.touch && Math.abs(dy) > Math.abs(dx) ? "scroll" : "pan";
        if (g.type === "pan") over.classList.add("dragging");
      }
      if (g.type === "pan") view.setRight(g.r0 - dx * view.win / plotW());
    });
    const end = (e) => {
      if (!pts.has(e.pointerId)) return;
      const single = pts.size === 1;
      pts.delete(e.pointerId);
      over.classList.remove("dragging");
      if (g && g.type === "maybe" && single && e.type === "pointerup" && performance.now() - g.t0 < 600) this.opts.onTap?.(tAt(g.x0));
      if (pts.size === 0 || (g && g.type === "pinch")) g = null;
    };
    over.addEventListener("pointerup", end);
    over.addEventListener("pointercancel", end);
    over.addEventListener("wheel", (e) => {
      if (e.ctrlKey || e.metaKey) {
        e.preventDefault();
        view.setWin(view.win * Math.exp(e.deltaY * 0.01), view.live ? null : tAt(e.offsetX));
      } else if (e.shiftKey || Math.abs(e.deltaX) > Math.abs(e.deltaY)) {
        e.preventDefault();
        const d = e.shiftKey && !e.deltaX ? e.deltaY : e.deltaX;
        view.setRight(view.right + d * view.win / plotW());
      }
    }, { passive: false });
  }
  _wireGutter(c) {
    const gEl = c.gutter, spec = c.spec;
    let g = null;
    gEl.addEventListener("wheel", (e) => {
      e.preventDefault();
      const r = gEl.getBoundingClientRect();
      this.zoomY(spec, Math.exp(e.deltaY * 0.002), c.u.posToVal(e.clientY - r.top, "y"));
    }, { passive: false });
    gEl.addEventListener("pointerdown", (e) => { gEl.setPointerCapture(e.pointerId); const y = this.y(spec); g = { y0: e.clientY, span: y.max - y.min, moved: false, touch: e.pointerType === "touch" }; });
    gEl.addEventListener("pointermove", (e) => {
      if (!g || g.touch) return;
      const dy = e.clientY - g.y0;
      if (Math.abs(dy) > 3) g.moved = true;
      if (g.moved) { this.panY(spec, dy * g.span / (c.u.bbox.height / uPlot.pxRatio)); g.y0 = e.clientY; }
    });
    gEl.addEventListener("pointerup", () => { if (g && !g.moved) this.opts.onOpenY?.(c); g = null; });
    gEl.addEventListener("pointercancel", () => { g = null; });
  }
}

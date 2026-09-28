// 収録 CSV(DevKit の形式。.csv / .csv.gz)を少しずつ読み、グラフに使う列を RowStore へ入れる。
//
// - gzip は fflate の Gunzip で流しながら展開する。実機アプリの .csv.gz は約 1 秒ごとに gzip をつなげた形で、
//   ブラウザの DecompressionStream は最初の 1 個で止まる。fflate は最後まで読む。
//   **fflate へは 64 KB ずつ渡す**(大きなかけらのままだと gzip の境目ごとに残りを写し直してメモリが膨らむ。
//   python-processing-core docs/porting-html/devkit-webview.md §9-2)。
// - 数字はバイトのまま読む(1 行ごとに文字列を作らない。ゴミでヒープが膨らむのを避ける)。
// - ヘッダの読み方は Mac 版 CsvReplayService.parse と同じ(Data mode / Transmission speed / 旧形式の Data quality /
//   Acceleration・Accelerometer sensor's range / Gyroscope sensor's range、//ARTIFACT 行が列名)。
import { Gunzip } from "./vendor/fflate.js";
import { RowStore } from "./store.js";

/** グラフに使う列(これ以外の列は読まない) */
export const CHART_COLUMNS = ["ACC_X", "ACC_Y", "ACC_Z", "GYRO_X", "GYRO_Y", "GYRO_Z",
  "EOG_L", "EOG_R", "EOG_H", "EOG_V", "EOG_L1", "EOG_R1", "EOG_L2", "EOG_R2", "EOG_H1", "EOG_H2", "EOG_V1", "EOG_V2"];

const MODE = { Standard: "standard", Full: "full", Quaternion: "quaternion" };
const afterColon = (line) => { const k = line.lastIndexOf(":"); return k < 0 ? "" : line.slice(k + 1).trim(); };

/** "yyyy/MM/dd HH:mm:ss.SS"(UTC)→ epoch ms。読めなければ null */
export function parseUtcDate(s) {
  const m = /^(\d{4})\/(\d{2})\/(\d{2}) (\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?/.exec(s.trim());
  if (!m) return null;
  const frac = m[7] ? Number(`0.${m[7]}`) : 0;
  return Date.UTC(+m[1], +m[2] - 1, +m[3], +m[4], +m[5], +m[6]) + Math.round(frac * 1000);
}

/**
 * stream: ReadableStream<Uint8Array>(fetch の body か file.stream())。total: 全体のバイト数(進み具合用。不明なら 0)。
 * 返り値: { mode, cps, accRange, gyroRange, columns, store, artifacts: [{row, text}], startedAt, warning }
 */
export async function readCsv(stream, { total = 0, onProgress = () => {} } = {}) {
  const reader = stream.getReader();
  const dec = new TextDecoder();
  const header = { mode: "", speed: "", quality: "", acc: "", gyro: "" };
  let colIndex = null, store = null, cols = [], srcIdx = null, artCol = -1, dateCol = -1;
  let startedAt = null, prevText = "", nFields = 0;
  const artifacts = [];
  let carry = new Uint8Array(0), read = 0, lastProgress = 0;
  let vals = null;

  const headerLine = (text) => {
    if (text.startsWith("//ARTIFACT")) {
      const names = text.slice(2).split(",").map((s) => s.trim());
      nFields = names.length;
      colIndex = Object.fromEntries(names.map((n, k) => [n, k]));
      cols = CHART_COLUMNS.filter((c) => c in colIndex);
      srcIdx = new Int16Array(nFields).fill(-1);
      cols.forEach((c, k) => { srcIdx[colIndex[c]] = k; });
      artCol = colIndex.ARTIFACT ?? -1; dateCol = colIndex.DATE ?? -1;
      const mode = MODE[header.mode];
      const speed = header.speed || (header.quality === "High" ? "100Hz" : header.quality === "Standard" ? "50Hz" : "");
      if (!mode || !speed) throw new Error("DevKit の CSV ではありません(ヘッダの Data mode / Transmission speed が読めません)");
      store = new RowStore(cols, speed === "100Hz" ? 100 : 50);
      store.meta = { mode, accRange: parseInt(header.acc, 10) || 8, gyroRange: parseInt(header.gyro, 10) || 1000 };
      vals = new Array(cols.length).fill(0);
    } else if (text.startsWith("// Data mode")) header.mode = afterColon(text);
    else if (text.startsWith("// Transmission speed")) header.speed = afterColon(text);
    else if (text.startsWith("// Data quality")) header.quality = afterColon(text);
    else if (text.startsWith("// Acceleration sensor's range") || text.startsWith("// Accelerometer sensor's range")) header.acc = afterColon(text);
    else if (text.startsWith("// Gyroscope sensor's range")) header.gyro = afterColon(text);
  };

  // 1 行(bytes[s, e))
  const line = (b, s, e) => {
    if (e > s && b[e - 1] === 13) e--;
    if (e <= s) return;
    if (!store) { headerLine(dec.decode(b.subarray(s, e))); return; }
    let f = 0, i = s, aS = -1, aE = -1, dS = -1, dE = -1;
    while (f < nFields && i <= e) {
      const fs = i;
      while (i < e && b[i] !== 44) i++;                 // ","
      const k = srcIdx[f];
      if (k >= 0) {
        let p = fs, neg = false, v = 0;
        if (b[p] === 45) { neg = true; p++; }
        for (; p < i; p++) { const d = b[p] - 48; if (d >= 0 && d <= 9) v = v * 10 + d; else break; }
        vals[k] = neg ? -v : v;
      } else if (f === artCol) { aS = fs; aE = i; }
      else if (f === dateCol && startedAt == null) { dS = fs; dE = i; }
      f++; i++;
    }
    if (dS >= 0) startedAt = parseUtcDate(dec.decode(b.subarray(dS, dE)));
    const row = store.n;
    store.push(vals);
    // ARTIFACT 列: 空でない値が続く区間ごとに、先頭の行と文字
    const text = aE > aS ? dec.decode(b.subarray(aS, aE)).trim() : "";
    if (text && text !== prevText) artifacts.push({ row, text });
    prevText = text;
  };

  const feed = (b) => {
    let s = 0;
    if (carry.length) {
      const e = b.indexOf(10);
      if (e < 0) { const t = new Uint8Array(carry.length + b.length); t.set(carry); t.set(b, carry.length); carry = t; return; }
      const t = new Uint8Array(carry.length + e); t.set(carry); t.set(b.subarray(0, e), carry.length);
      line(t, 0, t.length); s = e + 1; carry = new Uint8Array(0);
    }
    for (;;) { const e = b.indexOf(10, s); if (e < 0) break; line(b, s, e); s = e + 1; }
    if (s < b.length) carry = b.slice(s);
  };

  let first = true, gz = null, warning = "";
  try {
    for (;;) {
      const { value, done } = await reader.read();
      if (done) break;
      read += value.length;
      if (first) { first = false; if (value[0] === 0x1f && value[1] === 0x8b) gz = new Gunzip((d) => feed(d)); }
      if (gz) for (let k = 0; k < value.length; k += 65536) gz.push(value.subarray(k, k + 65536), false);
      else feed(value);
      if (read - lastProgress > (1 << 20)) { lastProgress = read; onProgress(read, total, store?.n ?? 0); await null; }
    }
    if (gz) gz.push(new Uint8Array(0), true);
    if (carry.length) line(carry, 0, carry.length);
  } catch (e) {
    if (!store) throw e;
    warning = `ファイルの末尾が途中で切れていたので、読めたところまでを開きました(${e.message})`;
  }
  if (!store) throw new Error("DevKit の CSV ではありません(//ARTIFACT の行がありません)");
  onProgress(read, total, store.n);
  return { ...store.meta, cps: store.cps, columns: cols, store, artifacts, startedAt, warning };
}

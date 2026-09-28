// 受けた行(1 行 = 1 パケット = CSV の 1 データ行)を、生の LSB のまま Int16 のブロックで持つ。
//
// 行番号 row は「アプリのサンプル番号 i − 最初の i」(再生では CSV のデータ行の番号)。時刻は row / cps 秒で、
// 行の間隔は一定とみなす(今のアプリのグラフと同じ)。i が飛んだら直前の値で埋めて時刻をそろえる。
// 実機(ライブ)は直近 capRows 行だけ持ち、古いブロックから捨てる。再生はファイルの全長を持つ
// (8.6 時間 × Full の 8 列で約 50 MB。python-processing-core docs/porting-html/devkit-webview.md §5-2)。

const BLOCK = 1 << 16;

export class RowStore {
  /** columns: 持つ列の名前(DevKit の CSV の列名)。cps: 1 秒あたりの行数。capRows: 持つ行数の上限 */
  constructor(columns, cps, { capRows = Infinity } = {}) {
    this.columns = columns;
    this.ncol = columns.length;
    this.index = Object.fromEntries(columns.map((c, k) => [c, k]));
    this.cps = cps;
    this.capRows = capRows;
    this.blocks = [];        // Int16Array(BLOCK * ncol)
    this.firstBlock = 0;     // blocks[0] が何番目のブロックか(捨てたぶん)
    this.n = 0;              // 受けた行数(= 次の行番号)
    this.i0 = null;          // 最初の行のアプリのサンプル番号
    this.overflow = 0;       // Int16 に収まらなかった値の数
    this.version = 0;
  }

  has(col) { return col in this.index; }
  get first() { return this.firstBlock * BLOCK; }          // 持っている最初の行
  get last() { return this.n - 1; }
  get seconds() { return this.n / this.cps; }
  rowOfI(i) { return this.i0 == null ? 0 : i - this.i0; }
  iOfRow(row) { return (this.i0 ?? 0) + row; }

  _put(row, vals) {
    const b = Math.floor(row / BLOCK) - this.firstBlock;
    while (this.blocks.length <= b) this.blocks.push(new Int16Array(BLOCK * this.ncol));
    const blk = this.blocks[b], o = (row % BLOCK) * this.ncol;
    for (let c = 0; c < this.ncol; c++) {
      const v = vals[c];
      if (v > 32767 || v < -32768) this.overflow++;
      blk[o + c] = v;
    }
  }

  /** 1 行を足す。i はアプリのサンプル番号(再生では省略して連番) */
  push(vals, i = null) {
    if (i != null) {
      if (this.i0 == null) this.i0 = i;
      const row = i - this.i0;
      if (row < this.n) return;                           // 古い・重複は捨てる
      while (this.n < row) { this._put(this.n, this._lastVals ?? vals); this.n++; }   // 飛んだぶんを埋める
    } else if (this.i0 == null) this.i0 = 0;
    this._put(this.n, vals);
    this._lastVals = vals;
    this.n++;
    if (this.n - this.first > this.capRows + BLOCK) { this.blocks.shift(); this.firstBlock++; }
    this.version++;
  }

  /** 列 col の [r0, r1) を out(Float32Array)へ。scale / offset を掛けて物理量にする */
  read(col, r0, r1, out, scale = 1, offset = 0) {
    const c = this.index[col];
    for (let r = r0, k = 0; r < r1; r++, k++) {
      const b = Math.floor(r / BLOCK) - this.firstBlock;
      out[k] = (this.blocks[b][(r % BLOCK) * this.ncol + c] + offset) * scale;
    }
    return out;
  }

  /** row の生の値 */
  value(col, row) {
    const b = Math.floor(row / BLOCK) - this.firstBlock;
    return this.blocks[b]?.[(row % BLOCK) * this.ncol + this.index[col]];
  }

  /** 表示する時間 [t0, t1] に当たる行の範囲(両端 1 行ずつ余分に)。持っていない所は切り詰める */
  span(t0, t1) {
    const r0 = Math.max(this.first, Math.floor(t0 * this.cps) - 1);
    const r1 = Math.min(this.n, Math.ceil(t1 * this.cps) + 2);
    return r1 > r0 ? [r0, r1] : [r0, r0];
  }

  bytes() { return this.blocks.length * BLOCK * this.ncol * 2; }
}

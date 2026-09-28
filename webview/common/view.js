// 表示する時間の範囲(全グラフ共通)。python-processing-core docs/porting-html/devkit-webview.md §3-5。
//
//   live  … 右端が最新(ライブ)/ 再生位置(再生)を追う。
//   ライブで左へ動かすと live を外れて過去を見る(受信は続く)。右端まで戻すと live に戻る。
//   再生では動かす = 再生位置を動かす(seek を渡す)。
// LIVE のまま時間を拡大・縮小したときは右端を保つ。live でなければ指・カーソルの位置を中心にする。

export class View {
  constructor({ defWin, maxWin, minWin = 2 }) {
    this.defWin = defWin; this.maxWin = maxWin; this.minWin = minWin;
    this.win = defWin;
    this.live = true;
    this._right = 0;
    this.head = () => 0;        // 最新 / 再生位置 [s]
    this.oldest = () => 0;      // 持っている最初の時刻 [s]
    this.seek = null;           // 再生のとき: (t) => 再生位置を t へ
  }
  clampWin(w) { return Math.max(this.minWin, Math.min(this.maxWin, w)); }
  get right() { return this.live ? this.head() : this._right; }
  get left() { return this.right - this.win; }

  setRight(r) {
    if (this.seek) { this.seek(Math.max(this.oldest() + Math.min(this.win, this.head()), r)); return; }
    const head = this.head();
    if (r >= head - 1e-6) { this.live = true; return; }
    this.live = false;
    this._right = Math.max(this.oldest() + this.win, Math.min(head, r));
  }
  /** 時間の拡大・縮小。anchorT は中心にする時刻(live のときは無視して右端を保つ) */
  setWin(w, anchorT = null) {
    const nw = this.clampWin(w);
    if (this.live || anchorT == null) { this.win = nw; return; }
    const r = this.right, f = (anchorT - (r - this.win)) / this.win;
    this.win = nw;
    this.setRight(anchorT - f * nw + nw);
  }
  goLive() { this.live = true; }
  reset() { this.win = this.defWin; this.live = true; }
  get modified() { return !this.live || Math.abs(this.win - this.defWin) > 1e-6; }
}

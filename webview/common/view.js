// 表示する時間の範囲(全グラフ共通)。python-processing-core docs/porting-html/devkit-webview.md §3-5。
//
//   live  … 右端が最新(ライブ)/ 再生位置(再生)を追う。
//   ライブで左へ動かすと live を外れて過去を見る(受信は続く)。右端まで戻すと live に戻る。
//   再生では動かす = 再生位置を動かす(seek を渡す)。
// LIVE のまま時間を拡大・縮小したときは右端を保つ。live でなければ指・カーソルの位置を中心にする。
//
// ライブの右端は最新サンプルそのものではなく、時計で等速に進めて最新の LIVE_LAG 秒後ろを追う(tick)。
// 行はアプリから 0.05〜0.06 秒ごと(BLE の届き方によってはもっと不揃い)にまとめて届くので、
// 最新に合わせると数コマ止まってはまとめて進む階段状の流れになる。

const LIVE_LAG = 0.15;      // 最新からの遅れ [s](まとめて届く間隔より長く)
const LIVE_FOLLOW = 0.5;    // ずれを寄せる時定数 [s]
const LIVE_SNAP = 1;        // これ以上ずれたら寄せずに合わせる(始まり・途切れの後)[s]

export class View {
  constructor({ defWin, maxWin, minWin = 2 }) {
    this.defWin = defWin; this.maxWin = maxWin; this.minWin = minWin;
    this.win = defWin;
    this.live = true;
    this._right = 0;
    this._shown = 0;            // ライブで表示している右端 [s](tick で進める)
    this.head = () => 0;        // 最新 / 再生位置 [s]
    this.oldest = () => 0;      // 持っている最初の時刻 [s]
    this.seek = null;           // 再生のとき: (t) => 再生位置を t へ
  }
  clampWin(w) { return Math.max(this.minWin, Math.min(this.maxWin, w)); }
  /** live のときの右端。再生は再生位置そのもの(時計で進んでいる)、ライブは tick で均した値 */
  get edge() { return this.seek ? this.head() : Math.min(this._shown, this.head()); }
  get right() { return this.live ? this.edge : this._right; }

  /** 毎コマ呼ぶ。dt は前のコマからの経過 [s] */
  tick(dt) {
    const head = this.head();
    if (this.seek) { this._shown = head; return; }
    const target = head - LIVE_LAG;
    let s = this._shown + dt;
    const err = target - s;
    if (!(Math.abs(err) < LIVE_SNAP)) s = target;
    else s += err * Math.min(1, dt / LIVE_FOLLOW);
    this._shown = Math.max(0, Math.min(head, s));
  }
  get left() { return this.right - this.win; }

  setRight(r) {
    if (this.seek) { this.seek(Math.max(this.oldest() + Math.min(this.win, this.head()), r)); return; }
    const edge = this.edge;
    if (r >= edge - 1e-6) { this.live = true; return; }
    this.live = false;
    this._right = Math.max(this.oldest() + this.win, Math.min(edge, r));
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
  reset() { this.win = this.defWin; this.live = true; this._shown = 0; }
  get modified() { return !this.live || Math.abs(this.win - this.defWin) > 1e-6; }
}

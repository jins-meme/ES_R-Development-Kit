# ES_R Development Kit for Windows（フル機能ロガー）

JINS MEME ES_R を Windows 本体の BLE Central で扱う、計測用のロガーです。
Mac 版 `ES_R_DevKit_Mac` に相当します。最小構成のサンプルが欲しい場合は
[`ES_R_DevKit_Windows_Simple`](../ES_R_DevKit_Windows_Simple/README.md) を見てください。

環境要件とビルド手順は [windows/README.md](../README.md) にまとめてあります。

```
dotnet build
dotnet run --project MEME_Academic_Sample
dotnet test
```

成果物は `MEME_Academic_Sample/bin/Debug/net10.0-windows10.0.22621.0/JINS_MEME_DataLogger.exe`。
プロジェクトのフォルダ名(`MEME_Academic_Sample`)と実行ファイル名(`JINS_MEME_DataLogger.exe`)は
別なので注意してください。exe 単体では動かないので、フォルダごと扱ってください。

## 画面

左カラムに接続と計測の操作、右側にグラフ画面を置きます（Mac 版 `ContentView` と同じ構成）。
グラフ画面は **WebView2 の中の Web ページ**（`webview/` の標準版 zip、または設定で選んだ zip）で、
3 アプリ（Mac / Windows / Android）で同じものです。ページとアプリの取り決めは
[`webview/BRIDGE.md`](../../webview/BRIDGE.md)。

- **Setting (S) メニュー** — 保存先や TCP 出力、グラフ画面の中身などの設定（[Setting](#setting) 参照）
- **バージョン表示** — アプリと ES_R ファームウェアのバージョン
- **Scan → デバイス選択 → Connect** — 接続状態は `State :` に出る。
  接続中に `Disconnect` を 5 秒押したままにすると Shelf mode へ移行できる
  （[Shelf mode](#shelf-mode) 参照）
- **File Replay** — 記録済み CSV を読み込んで再生する（[File Replay](#file-replay) 参照）
- **Select Mode / Trans Speed / Accel Range / Gyro Range** — 接続時に端末の現在値を読み出して反映する
- **Start Measurement** — 計測と CSV 記録の開始・停止
- **Free Marking** — 直後の 1 行の ARTIFACT 列に `X` を入れる。押した位置はグラフにも印として出る
- **Success rate / Communication** — 受信の成功率と直近 1 秒の通信率
- **グラフ画面** — 表示幅（60 / 30 / 15 / 10 秒）、◀◀ ▶▶ と LIVE、グラフごとの縦の拡大・縮小（− / + / Auto / ↺）、
  畳む・開く。Ctrl + ホイールで時間の拡大・縮小、ドラッグ / Shift + ホイールで前後に動かす。
  計測中・再生中はクリックで Artifact を付けられる（[Artifact](#artifact) 参照）。
  データのあるグラフだけを出す（Full = EOG・加速度・角速度 / Standard = EOG・加速度 / Quaternion = なし）

波形は間引かずに全サンプルを描いています。ハム（50/60Hz）成分を残して
電極の状態を目視で判断できるようにするためです。

**WebView2 ランタイム**は Windows 11 に最初から入っているので同梱しません。無い環境ではグラフの場所に
入手先の案内を出します。ページはアプリの中から `https://app.memeview.example/` として配り、
**ページから外へは通信させません**（全応答に Content-Security-Policy を付け、他のオリジンへの要求は断り、
WebRTC は読み込みの最初に消し、外のページへの移動と新しい窓は開かない）。開発者ツール（F12）は Debug ビルドだけです。

## 構成

| プロジェクト | 役割 |
|---|---|
| `MEMELib_Academic` | BLE 接続とプロトコル、CSV ファイルの読み書き |
| `MEME_Academic_Sample` | ロガー本体（WinForms） |
| `MEMELib_Academic.Tests` | 暗号化・パケット解析・CSV 読み書きの単体テスト |
| `MEME_Academic_Sample.Tests` | ロガー本体の単体テスト(CSV のヘッダと行の書式・OS の言語によらないこと・Artifact の無害化と書き戻し) |

`MEME_Academic_Sample` の構成:

| ファイル | 内容 |
|---|---|
| `MainForm.cs` | 画面の状態遷移と操作。Mac 版 `MEMEViewModel` に対応 |
| `Services/WebBridge.cs` | グラフ画面（WebView2）とのやり取り・配信・通信の制限。Mac 版 `WebBridge.swift` に対応 |
| `Services/WebContentStore.cs` | グラフ画面の中身（zip）の展開・切り替え。同梱の `WebContent/standard.zip` |
| `Services/ZipExtractor.cs` | zip の検査と展開（zip slip・zip 爆弾・リンクなどを展開前に断る。規則は `webview/BRIDGE.md` の Limits） |
| `Models/MeasurementMode.cs` | モードごとの列名・並びとレンジの表(CSV のヘッダと行、グラフへ渡す列と値、左の欄の選択肢が同じ表を見る) |
| `Services/ArtifactBuffer.cs` | ページで付けた Artifact の無害化と、CSV のデータ行の番号への換算 |
| `Services/CommunicationStatsTracker.cs` | 成功率・通信率の集計 |
| `Services/DataPersistenceService.cs` | CSV のヘッダ生成・行整形・バッファ保存 |
| `Services/TcpOutputServer.cs` | TCP による外部出力 |
| `Services/CsvArtifactWriter.cs` | Artifact の CSV への書き戻し |
| `SettingsForm.cs` | Setting ダイアログ |
| `MainForm.AutoTest.cs` | Debug ビルドだけの自己テスト（[自己テスト](#自己テストdebug-ビルド) 参照） |
| `ShelfModeForm.cs` | Shelf mode の確認ダイアログ |
| `UI/UiTheme.cs` | 角丸半径・枠線色。Mac 版の `cornerRadius: 6` に合わせてある |
| `UI/RoundedButton.cs` | 角丸ボタン。標準ボタンは直角なので自前で描く |

## File Replay

`File Replay` で CSV を選ぶと、その場で再生が始まります。**再生はグラフ画面（ページ）が受け持ちます**
（アプリはファイルをページに渡すだけ）。ページが読み終えると Select Mode / Trans Speed / Accel Range /
Gyro Range がファイルの記録条件に切り替わり、`State :` にファイル名が出ます。

- 再生・一時停止・速度・位置の操作はグラフ画面の下の操作バーにあります。
- CSV の `ARTIFACT` 列に値がある行は、グラフ上に縦線とラベルで重ねて表示します。
- `Disconnect` で再生を終えます（付けた Artifact はそのとき書き戻す）。
- 読み込めるのは本アプリ形式（Mac 版・Android 版と共通）の CSV です。`.csv` と `.csv.gz` のどちらも開けます。
- エクスプローラーで `.csv` / `.csv.gz` を右クリック →「プログラムから開く」からも起動できます。

## Shelf mode

Shelf mode（保管モード）は、ペアリング機能を止めて消費電力を抑える端末側のモードです。
出荷前や長期保管の前に使います。**復帰は充電のみで、アプリからは戻せません。**

接続中かつ非計測のときに `Disconnect` を **5 秒押したまま**にすると確認ダイアログが出て、
`Yes` を選ぶと移行します。誤操作を防ぐための隠し操作なので、押している間のゲージ表示などは
出しません。移行の手順は Mac 版・Web Bluetooth 版 SDK と同じです。

1. CONFIG モードへの遷移（`ADN_SET_MODE` の mode=0x0F）を送る
2. その ACK（`0x8F`、3 秒でタイムアウト）を待つ
3. SHELF コマンド（`0x41` + ASCII `"SHELF"`）を送る
4. 端末が自ら切断したら成功（5 秒待っても切断されなければ失敗）

ACK が返らなければ SHELF は送らないので、失敗しても端末は通常モードのままです。

## Artifact

計測中または再生中にグラフをクリックすると、グラフ画面の中に入力欄が出ます。空のまま確定すると
`X` が入ります。付けた印はその場でグラフへ表示され、CSV へは次のタイミングでまとめて書き戻します。

| 状況 | 書き戻すタイミング | 書き戻し先 |
|---|---|---|
| 計測中 | `Stop Measurement` / 切断 | その計測で保存した CSV |
| 再生中 | `Save Artifacts` / `Disconnect` | 再生元の CSV |

- カンマと改行は列が崩れないよう空白へ置き換えます（64 文字まで）。
- `=` `+` `-` `@` で始まる文字は受けません（表計算ソフトで数式として読まれるため。ページも入力時に断る）。
- 同じ行に何度付けても、最後に入力した値で上書きされます。
- `Free Marking` の `X` は受信時にその場で CSV へ書くので、書き戻しの対象にはなりません。
- 書き戻しは一時ファイルへ書いてから置き換えるので、途中で失敗しても元の CSV は壊れません。

以前あった「ドラッグで区間を切り出す」は無くなりました（Artifact で区間の印を付けて代わりにする）。

## Setting

メニューバーの `Setting (S)` で開きます。内容は `%APPDATA%\JINS\MEME_Academic\settings.json` に保存され、
次回起動時に復元されます。

| 項目 | 内容 |
|---|---|
| Save File Path | CSV の保存先。既定は `ドキュメント\JINS\MEME_Academic` |
| Acc Offset X / Y / Z | グラフ表示のみに足すオフセット（LSB）。CSV の値は変えない |
| Save Format | 計測データを gzip 圧縮して保存する（既定 ON）。ON なら `.csv.gz`、OFF なら `.csv` |
| Save Dialog | 計測終了後に保存先を選び直すダイアログを出す |
| Time Display | グラフの時刻をローカルタイムで表示する（記録は常に UTC） |
| TCP Output | 計測データを TCP で外部へ流す |
| Local Port | 待ち受けポート。既定は 88 |
| Display Engine | グラフ画面の中身。`Choose zip…` で zip を選ぶとその場で検査して取り込み（通らなければ理由を赤で出し、今のまま）、`Use Built-in` で同梱の標準版に戻す。計測中・再生中は切り替えられない。展開先は `%LOCALAPPDATA%\JINS\MEME_Academic\WebContent` |

## TCP 出力

`TCP Output` を ON にすると、指定ポートで待ち受けを始めます（左カラムの `Status :` が
`Listen` になります）。クライアントが 1 台つながると `Accepted` になり、CSV とまったく
同じ書式のヘッダと行が流れます。計測開始より前に接続していた場合は、計測開始時に
ヘッダが送られます。同時に受け付けるのは 1 台までです。受け取る側が読まなくなり、1 秒待っても送れないときは
そのクライアントを切ります（計測と CSV の記録は止めません）。

```
$ ncat 127.0.0.1 88
// Data mode  : Full
// Transmission speed  : 100Hz
// Acceleration sensor's range  : 2g
// Gyroscope sensor's range  : 250dps
//
//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V
,1,2026/08/27 04:27:10.21,-200,-3415,-2284,178,-343,763,2007,2001,6,-2004
```

## CSV

Setting の保存先に `<MACアドレス>_<UTC日時>.csv.gz` として出力します（Setting の Save Format を
OFF にすると `.csv`）。書式は Mac 版・Android 版と共通で、モードごとに列が変わります。

| モード | 列 |
|---|---|
| Standard | `ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2` |
| Full | `ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V` |
| Quaternion | `ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z` |

- `DATE` は UTC。ローカルタイム表示は Setting の Time Display で切り替える（表示のみ）。
- `NUM` は端末カウンタの差分を積算した単調増加値。取りこぼしがあると番号が飛ぶ。
- `ARTIFACT` は `Free Marking` を押した直後の 1 行に `X` が入る。
- 100Hz なら 100 行、50Hz なら 50 行たまるごとに書き出します。1 行ずつ open/close すると
  取りこぼすためで、計測停止時に残りをフラッシュします。
- `.csv.gz` の場合、この 1 回の書き出しが gzip の 1 メンバーになり、それをファイルへ
  連結していきます。gzip は複数メンバーの連結を 1 ファイルとして扱えるので、`gzip -d` でも
  本アプリの File Replay でもそのまま読めます。ストリームを開きっぱなしにして最後に
  トレーラを書く方式と違い、アプリが落ちても切断で計測が途切れても、その時点までの
  ファイルが常に完結しています（圧縮率の悪化は実測で数％）。

## Mac 版との対応状況

Mac 版の機能は段階的に移植し、現時点で一通り揃っています。

| 機能 | 状態 |
|---|---|
| グラフ画面（WebView2 + uPlot。表示幅・拡大縮小・畳む。zip で差し替え） | 実装済み |
| 通信統計 | 実装済み |
| Setting ダイアログ（保存先、Acc オフセット、TCP 出力、ローカルタイム表示） | 実装済み（メニューバーの `Setting (S)`） |
| CSV のバッファ保存・保存先指定・計測後の保存ダイアログ | 実装済み |
| TCP ソケットによる外部出力 | 実装済み |
| Standard / Quaternion モード（0x98 / 0x9A） | 実装済み |
| File Replay（再生・一時停止・シーク・速度切替。ページ側） | 実装済み |
| グラフのクリックでの Artifact 付与 | 実装済み（範囲切り出しは廃止） |
| CSV の gz 圧縮保存（`.csv` / `.csv.gz` の読み込み） | 実装済み |
| `Disconnect` 長押しでの Shelf mode 移行 | 実装済み |

Quaternion モードにはグラフに出せる波形が無いため、グラフ画面は「このモードで表示するグラフはありません」を出します
（Mac 版も同じ）。Standard モードは 1 パケットに EOG が 2 サンプルあり、ページが両方を描きます。

## 自己テスト（Debug ビルド）

画面を操作できない環境（Mac から Parallels の Windows を動かすときなど）から、グラフ画面が動くことを確かめる口です。
結果（`result.json`）とスナップショット（PNG）を指定のフォルダに書いて、アプリが自分で閉じます。Release ビルドには入りません。

```
JINS_MEME_DataLogger.exe --autotest <出力フォルダ> [--suite live|replay|zip|settings|webcrash|reconnect] [--csv <CSV>] [--zip <zip>]
                         [--mode full|standard] [--seconds 20] [--badzips <フォルダ>] [--probe <JS の式 | @ファイル>]
                         [--probe-live <同>] [--expect <瞬目,EMR,EML[,歩のイベント]>] [--real [--device <アドレスか名前の末尾>]]
                         [--socket <ポート> [--socket-stall]]
```

`--probe` は各場面の後に、`--probe-live` は live で行を流している最中にページで評価します（描画の進み方を測るときなど）。

| suite | 中身 |
|---|---|
| `live`（既定） | 実機の代わりに `--csv` の行を 100 Hz で受信の口へ流し、計測 → Artifact → 停止 → 保存した CSV を再生（BLE には触らない）。保存した CSV のモード・番号（NUM）の抜け・Artifact の行（299）、高機能版の Standard では判定器が止まってトーストが出ること、再生中の Artifact が `Save Artifacts`（500 行目）と `Disconnect`（600 行目）で書き戻されることを確かめる。**`--real` を付けたときだけ実機（BLE）を使う**（`--device` の端末に繋いで `--seconds` 計測。`--csv` は要らない） |
| `replay` | `--csv` の写しを再生（高機能版なら最後まで解析を待つ）。Artifact を `Save Artifacts`（299 行目）と `Disconnect`（600 行目）で書き戻す。`--expect` で判定数を比べる（golden `w-sit-jump-stairs` なら `459,1169,1045,2837`） |
| `zip` | `--badzips` の zip を 1 つずつ取り込み、`good` で始まるものだけ通り、断られたものは今の中身が変わらず外に書かれないことを見る（`webview/tools/make_bad_zips.py`） |
| `settings` | 設定画面を撮る（計測中の形も） |
| `webcrash` | `--csv` の行を流して計測している最中と、その CSV を再生している最中に、グラフ画面のプロセスを落とす（DevTools の `Page.crash`）。読み込み直したページで計測・再生が続いていることを確かめる |
| `reconnect` | 計測中に切断 → 繋ぎ直して `Start Measurement` で計測が始まること、切断した回の CSV が停止と同じく締められること（ファイルが分かれる・NUM が続かない・付けた Artifact が書き戻される）を確かめる。`--real` なら実機、無ければ `--csv` の行を流す |

`--socket` はテストの間だけ TCP 出力をそのポートで有効にし、`live` の間テスト自身が受け取って、届いたヘッダと行が保存した CSV と
（ARTIFACT 列を除いて）同じかを見ます。`--socket-stall` を足すと受け取る側が読まないままにし、送信が詰まっても計測が止まらない
（詰まったクライアントが切られる）ことを見ます。

`--zip` は始める前に Display Engine と同じ経路で取り込み、終わったら同梱の標準版に戻します（もともと選んだ zip を使っていたら、
退避しておいたそれに戻す。`zip` の組も同じ）。保存先はテストの間だけ `<出力フォルダ>\csv` になります。
確かめたことが合わなければ `result.json` の `ok` が `false` になり、`error` に理由が入ります（Artifact の書き戻しの失敗も、
テストの間はダイアログを出さずにここへ入る）。

Mac から Parallels の Windows で回すときの注意:

- VM から見える Mac のフォルダは `Z:\`（`\\Mac\Home`）の Desktop・Documents・Downloads だけ。テストに使うファイルはそこに置く。
- **`prlctl exec` には `--current-user` を付ける**（ログイン中の利用者のセッションで動かす）。付けないと画面の無いセッションで
  起動して WebView2 が立ち上がらず（`The graph view could not start. … (0x800705B4)`）、アプリが閉じずに残る。
  `dotnet` もそのセッションでは見つからないことがあるので、`C:\Users\<利用者>\.dotnet\dotnet.exe` をフルパスで呼ぶ。
- `prlctl exec` は引数の引用符を落とすので、アプリへ渡す引数は PowerShell スクリプトの中で組み立てる（ファイルから読むなど）。
- `prlctl exec` に渡す PowerShell スクリプトは ASCII だけで書く（PowerShell 5.1 が BOM なしの UTF-8 を CP932 で読む）。
- ビルドは VM のローカル（`C:\work\…`）へ `robocopy /MIR /XD bin obj` で写してから行う。

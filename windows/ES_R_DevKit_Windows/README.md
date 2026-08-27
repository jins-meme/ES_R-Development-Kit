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

左カラムに接続と計測の操作、右側にリアルタイムチャートを 3 枚並べます
（Mac 版 `ContentView` と同じ構成）。

- **Setting (S) メニュー** — 保存先や TCP 出力などの設定（[Setting](#setting) 参照）
- **バージョン表示** — アプリと ES_R ファームウェアのバージョン
- **Scan → デバイス選択 → Connect** — 接続状態は `State :` に出る
- **File Replay** — 記録済み CSV を読み込んで再生する（[File Replay](#file-replay) 参照）
- **Select Mode / Trans Speed / Accel Range / Gyro Range** — 接続時に端末の現在値を読み出して反映する
- **Start Measurement** — 計測と CSV 記録の開始・停止
- **＋ / －** — チャートの X 軸レンジを 3 / 7 / 15 / 30 秒で切り替える。
  横軸の目盛りは丸い時刻(1 / 2 / 5 秒刻み、レンジに応じて自動選択)に打つので、
  ラベルと目盛り線が波形と同じ速さで左へ流れる
- **Free Marking** — 直後の 1 行の ARTIFACT 列に `x` を入れる
- **Success rate / Communication** — 受信の成功率と直近 1 秒の通信率
- **チャート** — カテゴリ選択 + Apply、系列ごとのチェックボックス、縦軸の拡大・縮小（`↕＋` / `↕－`）。
  計測中・再生中はクリックで Artifact を付けられ、再生中はドラッグで区間を切り出せる
  （[Artifact と区間切り出し](#artifact-と区間切り出し) 参照）

波形は間引かずに全サンプルを描いています。ハム（50/60Hz）成分を残して
電極の状態を目視で判断できるようにするためです。

## 構成

| プロジェクト | 役割 |
|---|---|
| `MEMELib_Academic` | BLE 接続とプロトコル |
| `MEME_Academic_Sample` | ロガー本体（WinForms） |
| `MEMELib_Academic.Tests` | 暗号化とパケット解析の単体テスト |

`MEME_Academic_Sample` の構成:

| ファイル | 内容 |
|---|---|
| `MainForm.cs` | 画面の状態遷移と操作。Mac 版 `MEMEViewModel` に対応 |
| `Charting/ChartModels.cs` | `ChartPlot` / `ChartSeries` / 系列トグル |
| `Charting/ChartCanvas.cs` | GDI+ による波形描画。Mac 版 `RealtimeChartView` に対応 |
| `Charting/ChartPanel.cs` | チャート 1 枚ぶんの UI。Mac 版 `ChartPanelView` に対応 |
| `Services/ChartService.cs` | 表示ウィンドウの切り出しと系列生成 |
| `Services/CommunicationStatsTracker.cs` | 成功率・通信率の集計 |
| `Services/DataPersistenceService.cs` | CSV のヘッダ生成・行整形・バッファ保存 |
| `Services/TcpOutputServer.cs` | TCP による外部出力 |
| `Services/CsvReplayService.cs` | 再生用 CSV の解析・再生タイマー・Artifact 書き戻し・区間切り出し |
| `SettingsForm.cs` | Setting ダイアログ |
| `ArtifactForm.cs` / `CutFileForm.cs` | Artifact 入力・区間切り出しのダイアログ |

## File Replay

`File Replay` で CSV を選ぶと、その場で再生が始まります（Mac 版と同じく Start Replay ボタンは
持ちません）。読み込むと Select Mode / Trans Speed / Accel Range / Gyro Range が
ファイルの記録条件に切り替わり、`State :` にファイル名が出ます。

| 操作 | 内容 |
|---|---|
| スライダー | 再生位置の変更。離した時点でシークする |
| `<<` / `>>` | X 軸レンジ − 2 秒ぶん戻る／進む。前後のウィンドウが 2 秒重なって見える |
| `x1` | 再生速度を x1 → x2 → x4 → x8 → x16 → x32 → x1 と切り替える |
| `Pause` / `Resume` | 一時停止と再開 |
| `Record` | 再生を止める。グラフはそのまま残る |
| `Disconnect` | 再生セッションを終える。読み込んだデータを破棄する |

- チャートの X 軸は CSV の `DATE` 列（UTC）を基準に描くので、記録時の時刻がそのまま出ます。
- シークやレンジ変更のあとは、右端から徐々に埋めるのではなくウィンドウ幅ぶんを先読みして
  満たした状態で再開します。
- CSV の `ARTIFACT` 列に値がある行は、チャート上に縦線とラベルで重ねて表示します。
- 再生中にチャートをドラッグすると、その区間だけを別の CSV へ切り出せます。
- 再生速度を上げても間引きはしません。1 秒あたりに流す行数を増やしているだけなので、
  波形の細部（ハム成分など）は x32 でも残ります。
- 読み込めるのは本アプリ形式（Mac 版・Android 版と共通）の CSV だけです。旧 Windows 版の
  `// Accelerometer sensor's range` や `// Data quality` 表記、`BattLv` 列付きの CSV も読めます。

## Artifact と区間切り出し

**Artifact** — 計測中または再生中にチャートをクリックすると入力ダイアログが出ます。空のまま OK で
`X` が入ります。付けた印はその場でチャートへ表示され、CSV へは次のタイミングでまとめて書き戻します。

| 状況 | 書き戻すタイミング | 書き戻し先 |
|---|---|---|
| 計測中 | `Stop Measurement` / 切断 | その計測で保存した CSV |
| 再生中 | `Record` / `Disconnect` | 再生元の CSV |

- カンマと改行は列が崩れないよう空白へ置き換えます。
- 同じ行を何度クリックしても、最後に入力した値で上書きされます。
- 計測中に付けた印は「サンプル位置 − 1」の行へ書きます。CSV は端末カウンタの基準取りに使う
  先頭 1 件を落としているためです。
- 書き戻しは一時ファイルへ書いてから置き換えるので、途中で失敗しても元の CSV は壊れません。

**区間切り出し** — 再生中にチャート上を横方向へドラッグすると、選択範囲が青く反転します。
離すとファイル名の入力ダイアログが出て、再生元と同じフォルダへその区間だけの CSV を書き出します。
ヘッダはそのままコピーするので、切り出した CSV もそのまま File Replay で開けます。
既に同名のファイルがある場合はダイアログを閉じずにエラーを表示します。

## Setting

メニューバーの `Setting (S)` で開きます。内容は `%APPDATA%\JINS\MEME_Academic\settings.json` に保存され、
次回起動時に復元されます。

| 項目 | 内容 |
|---|---|
| Save File Path | CSV の保存先。既定は `ドキュメント\JINS\MEME_Academic` |
| Acc Offset X / Y / Z | チャート表示のみに足すオフセット。CSV の値は変えない |
| Save Dialog | 計測終了後に保存先を選び直すダイアログを出す |
| Time Display | チャート X 軸をローカルタイムで表示する（記録は常に UTC） |
| TCP Output | 計測データを TCP で外部へ流す |
| Local Port | 待ち受けポート。既定は 88 |

## TCP 出力

`TCP Output` を ON にすると、指定ポートで待ち受けを始めます（左カラムの `Status :` が
`Listen` になります）。クライアントが 1 台つながると `Accepted` になり、CSV とまったく
同じ書式のヘッダと行が流れます。計測開始より前に接続していた場合は、計測開始時に
ヘッダが送られます。同時に受け付けるのは 1 台までです。

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

Setting の保存先に `<MACアドレス>_<UTC日時>.csv` として出力します。書式は Mac 版・Android 版と
共通で、モードごとに列が変わります。

| モード | 列 |
|---|---|
| Standard | `ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2` |
| Full | `ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V` |
| Quaternion | `ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z` |

- `DATE` は UTC。ローカルタイム表示は Setting の Time Display で切り替える（表示のみ）。
- `NUM` は端末カウンタの差分を積算した単調増加値。取りこぼしがあると番号が飛ぶ。
- `ARTIFACT` は `Free Marking` を押した直後の 1 行に `x` が入る。
- 100Hz なら 100 行、50Hz なら 50 行たまるごとに書き出します。1 行ずつ open/close すると
  取りこぼすためで、計測停止時に残りをフラッシュします。

## Mac 版との対応状況

Mac 版の機能は段階的に移植し、現時点で一通り揃っています。

| 機能 | 状態 |
|---|---|
| リアルタイムチャート 3 枚、通信統計、X/Y レンジ操作 | 実装済み |
| Setting ダイアログ（保存先、Acc オフセット、TCP 出力、ローカルタイム表示） | 実装済み（メニューバーの `Setting (S)`） |
| CSV のバッファ保存・保存先指定・計測後の保存ダイアログ | 実装済み |
| TCP ソケットによる外部出力 | 実装済み |
| Standard / Quaternion モード（0x98 / 0x9A） | 実装済み |
| File Replay（再生・一時停止・シーク・速度切替） | 実装済み |
| チャートタップでの Artifact 付与、範囲切り出し | 実装済み |

Quaternion モードにはチャートに出せる波形が無いため、計測中もチャートは空のままです
（Mac 版も同じ挙動）。Standard モードの EOG チャートは 1 組目（`EOG_L1` / `EOG_R1` /
`EOG_H1` / `EOG_V1`）を描きます。

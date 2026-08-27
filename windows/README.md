# ES_R-Development-Kit/Windows

## Summary

* "ES_R" means JINS MEME ES_R (previously called JINS MEME Academic Pack)
* **Dongle (BLE receiver) is NOT needed** — the PC's own Bluetooth LE radio is used
* Visual Studio is not required; everything builds from the command line with the .NET SDK

このディレクトリには 2 つのプロジェクトがあります。Mac 版の
`ES_R_DevKit_Mac` / `ES_R_DevKit_Mac_Simple` と同じ関係です。

| プロジェクト | 位置づけ |
|---|---|
| [`ES_R_DevKit_Windows`](ES_R_DevKit_Windows/README.md) | フル機能ロガー。リアルタイムチャート、通信統計などを備える |
| [`ES_R_DevKit_Windows_Simple`](ES_R_DevKit_Windows_Simple/README.md) | 最小構成のサンプル。接続してセンサー値を表示・CSV 保存するだけ |

BLE の接続処理とプロトコル(`MEMELib_Academic`)は両者で共通の構成です。
まず動かして仕組みを読むなら Simple、計測に使うなら フル機能版 を選んでください。

## 環境要件

| 項目 | 内容 |
|---|---|
| OS | Windows 10 バージョン 1809 以降 / Windows 11 |
| ハードウェア | BLE 対応の Bluetooth アダプタ(内蔵で可) |
| SDK | .NET 10 SDK (`winget install Microsoft.DotNet.SDK.10`) |
| IDE | 不要。VS Code + コマンドラインで完結する |

Windows SDK 本体のインストールは不要です。`net10.0-windows10.0.22621.0` を
ターゲットにしているため、WinRT の射影(`Microsoft.Windows.SDK.NET.Ref`)は
NuGet から自動で取得されます。**Visual Studio 2019 では .NET 10 を扱えません**。
WinForms のビジュアルデザイナを使いたい場合のみ Visual Studio 2022 が要ります。

## ビルドと実行

```
cd windows/ES_R_DevKit_Windows          # または ES_R_DevKit_Windows_Simple
dotnet build
dotnet run --project MEME_Academic_Sample
dotnet test
```

成果物は `MEME_Academic_Sample/bin/Debug/net10.0-windows10.0.22621.0/JINS_MEME_DataLogger.exe`。
プロジェクトのフォルダ名(`MEME_Academic_Sample`)と実行ファイル名(`JINS_MEME_DataLogger.exe`)は
別なので注意してください。exe 単体では動かないので、フォルダごと扱ってください。

## 接続手順

1. ES_R の電源ボタンを 2 秒長押ししてペアリングモードにする。
2. `Scan` を押す。見つかった端末が `ESRG2_0 (28A183055C47)` の形で一覧に出る。
3. `Connect` を押す。接続されると ES_R のファームウェアバージョンが表示される。
4. `Start Measurement` でセンサー値の取得と CSV 記録が始まる。

CSV の書式(ヘッダ・列順・UTC 記録)は Mac 版・Android 版と共通です。

## プロトコル

| 項目 | 値 |
|---|---|
| Service | `D6F25BD1-5B54-4360-96D8-7AA62E04C7EF` |
| Notify (端末 → PC) | `D6F25BD4-5B54-4360-96D8-7AA62E04C7EF` |
| Write (PC → 端末) | `D6F25BD2-5B54-4360-96D8-7AA62E04C7EF` |
| パケット長 | 20 byte 固定 |

接続後のハンドシェイクは
`0xA1 GetDevInfo` → `0x81` → `0xA3 GetMode` → `0x83` → `0xA9 Get6AxisParams` → `0x89`
の順で、`0x89` を受け取った時点で接続完了です。計測中は
`0x99 (AUP_REPORT_ACADEMIA2)` が 100Hz で届きます。

実装は Mac 版 `Mac/ES_R_DevKit_Mac_Simple/MEME_Academic/BLE/MEMELib_Academic.swift` と
Android 版 `android/ES_R_DevKit_Android2/core/src/main/java/com/jins_jp/meme/core/ble/`
に準拠しています。

## うまく動かないとき

- **`Scan` で何も出ない**: ES_R がペアリングモード(電源ボタン 2 秒長押し)に
  なっているか、Windows の Bluetooth が ON か確認してください。
- **他のアプリが掴んでいる**: ES_R は同時に 1 つのホストとしか繋がりません。
  スマートフォンのアプリなどが接続中なら切ってください。
- **接続はできるが値が来ない**: Windows の設定 > Bluetooth とデバイス から
  ES_R を一度削除し、再度スキャンし直すと GATT のキャッシュが解消することがあります。

## 旧サンプルについて

USB BLE ドングル(仮想 COM ポート)を前提とした .NET Framework 4.5.2 の旧サンプルは
削除しました。非公開の NuGet パッケージ `JINSMEME_ES_R` に依存しており、単体では
ビルドできない状態だったためです。内容が必要な場合は Git の履歴を参照してください。

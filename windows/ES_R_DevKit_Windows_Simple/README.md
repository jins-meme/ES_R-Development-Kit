# ES_R Development Kit for Windows 2

JINS MEME ES_R (JINS MEME Academic Pack) を **Windows 本体の BLE Central** で
直接扱うサンプルです。旧 `windows/ES_R_DevKit_Windows` が必要としていた
**USB BLE ドングル(仮想 COM ポート)は不要**になりました。

## 環境要件

| 項目 | 内容 |
|---|---|
| OS | Windows 10 バージョン 1809 以降 / Windows 11 |
| ハードウェア | BLE 対応の Bluetooth アダプタ(内蔵で可) |
| SDK | .NET 10 SDK |
| IDE | 不要。Visual Studio がなくてもコマンドラインで完結する |

Windows SDK 本体のインストールは不要です。`net10.0-windows10.0.22621.0` を
ターゲットにしているため、WinRT の射影(`Microsoft.Windows.SDK.NET.Ref`)は
NuGet から自動で取得されます。

### .NET 10 SDK の導入

```
winget install Microsoft.DotNet.SDK.10
```

導入後、新しいシェルで `dotnet --list-sdks` に 10.x が出れば準備完了です。

### エディタ(任意)

- **VS Code**: 拡張 `ms-dotnettools.csharp`(または C# Dev Kit)を入れると
  IntelliSense とデバッガが使えます。
- **Visual Studio 2022**: WinForms のビジュアルデザイナを使いたい場合のみ必要です。
  `.Designer.cs` は普通の C# なので、手で編集する限り VS は不要です。
  なお **Visual Studio 2019 では .NET 10 を扱えません**。

## ビルドと実行

```
cd windows/ES_R_DevKit_Windows2
dotnet build
dotnet run --project MEME_Academic_Sample
```

成果物は `MEME_Academic_Sample/bin/Debug/net10.0-windows10.0.22621.0/JINS_MEME_DataLogger.exe`。
プロジェクトのフォルダ名(`MEME_Academic_Sample`)と実行ファイル名(`JINS_MEME_DataLogger.exe`)は
別なので注意してください。exe 単体では動かないので、フォルダごと扱ってください。

テスト(BLE 実機は不要):

```
dotnet test
```

## 使い方

1. ES_R の電源ボタンを 2 秒長押ししてペアリングモードにする。
2. `Scan MEME` を押す。見つかった端末が `ESRG2_0 (28A183055C47)` の形で一覧に出る
   (最大 10 秒でタイムアウト)。
3. `Connect` を押す。`Status : Connected` になり、ステータスバーに ES_R の
   ファームウェアバージョンが出る。
4. Accelerometer / Gyroscope のレンジを選び、`Start Measurement` を押す。
5. `Result/<MACアドレス>_<UTC日時>.csv` にセンサー値が追記される。
   `Free Marking` を押すと、その直後の 1 行の ARTIFACT 列に `X` が入る。

CSV の書式(ヘッダ・列順・UTC 記録)は Mac 版・Android 版と共通です。

## 構成

| プロジェクト | 役割 |
|---|---|
| `MEMELib_Academic` | BLE 接続とプロトコル。旧 `MEMELib_Academic.dll` の置き換え |
| `MEME_Academic_Sample` | WinForms のサンプル UI |
| `MEMELib_Academic.Tests` | 暗号化とパケット解析の単体テスト |

`MEMELib_Academic` の中身:

| ファイル | 内容 |
|---|---|
| `MemeProtocol.cs` | GATT の UUID、ADN/AUP のオペコード、コマンド生成、パケット解析 |
| `DecEnc.cs` | 20 byte パケットの難読化(先頭 2 byte 以外を固定鍵で変換) |
| `MEMELib.cs` | WinRT (`Windows.Devices.Bluetooth`) を使った BLE Central の実装 |
| `CsvFileWriter.cs` | 100Hz の追記に耐える CSV ライタ |
| `MEMETypes.cs` | 公開列挙体と `AcademicFullData` |

## プロトコル

| 項目 | 値 |
|---|---|
| Service | `D6F25BD1-5B54-4360-96D8-7AA62E04C7EF` |
| Notify (端末 → PC) | `D6F25BD4-5B54-4360-96D8-7AA62E04C7EF` |
| Write (PC → 端末) | `D6F25BD2-5B54-4360-96D8-7AA62E04C7EF` |
| パケット長 | 20 byte 固定 |

接続後のハンドシェイクは
`0xA1 GetDevInfo` → `0x81` → `0xA3 GetMode` → `0x83` → `0xA9 Get6AxisParams` → `0x89`
の順で、`0x89` を受け取った時点で接続完了を通知します。計測中は
`0x99 (AUP_REPORT_ACADEMIA2)` が 100Hz で届きます。

実装は Mac 版 `Mac/ES_R_DevKit_Mac_Simple/MEME_Academic/BLE/MEMELib_Academic.swift` と
Android 版 `android/ES_R_DevKit_Android2/core/src/main/java/com/jins_jp/meme/core/ble/`
に準拠しています。

## 旧版 (`windows/ES_R_DevKit_Windows`) との違い

- USB ドングルと COM ポートの UI(`Scan port` / `Open`)を廃止。
- 非公開の `MEMELib_Academic.dll`(NuGet `JINSMEME_ES_R`)への依存を廃止し、
  同等の機能を C# で実装。
- .NET Framework 4.5.2 → .NET 10、csproj を SDK 形式へ移行。
- スキャン結果を MAC アドレスだけでなくデバイス名付きで表示。
- センサー値のラベル更新を 20Hz に間引き(CSV は 100Hz 全サンプルを記録)。

## うまく動かないとき

- **`Scan MEME` で何も出ない**: ES_R がペアリングモード(電源ボタン 2 秒長押し)に
  なっているか、Windows の Bluetooth が ON か確認してください。
- **他のアプリが掴んでいる**: ES_R は同時に 1 つのホストとしか繋がりません。
  スマートフォンのアプリなどが接続中なら切ってください。
- **接続はできるが値が来ない**: Windows の設定 > Bluetooth とデバイス から
  ES_R を一度削除し、再度スキャンし直すと GATT のキャッシュが解消することがあります。

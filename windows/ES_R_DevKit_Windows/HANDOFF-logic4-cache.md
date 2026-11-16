# 引き継ぎ: Windows 版の日別キャッシュ API と電池切れの SLEEP(2026-11-16)

Mac で書いた(.NET が無いので**ビルドもテストもしていない**)。Windows 機ではビルドから始めること。
ブランチ `feature/logic4-cache`。同じ変更は Android・iOS・Mac では済み・実機で確かめ済み。

## 変えたもの

| ファイル | 中身 |
| :--- | :--- |
| `MEME_Academic_Sample/Services/CacheStore.cs`(新規) | 日別キャッシュの一覧・読み・書き(`webview/BRIDGE.md` の Cache files)。Mac の `CacheStore.swift` と同じ規則(device / name の形・16 MB・`.tmp` に書いてから `File.Move(overwrite: true)`・エラーの名前)。置き場所は `CacheStore.RootFrom(setting)` = Graph Cache Folder、空なら Save File Path の下の `logic4-cache`(名前が `logic4-cache` のフォルダはそのもの)。求めは `SemaphoreSlim` で 1 本ずつ、`Task.Run` で UI の外 |
| `Services/WebBridge.cs` | コンストラクタに `cacheRoot` を足し `Cache` を持つ。ページ → アプリの `cacheList` / `cacheRead` / `cacheWrite` を `AnswerCacheAsync` で受け、UI スレッドで `jmasHost.cacheResult(...)` を返す。`OpenReplay` は `features: ["cache"]` と CSV 名の先頭の `device` を足す |
| `MainForm.cs` | `new WebBridge(..., () => CacheStore.RootFrom(setting))`。ライブの start の cond に `"cache"` と `device`(接続中の端末のアドレス)。ライブラリの `memeLowBatterySleep` を受けて時刻を覚え、30 秒以内の切断は状態の表示を「State : Disconnected (Battery empty: the glasses went to sleep (low voltage). Please charge them.)」に(Mac と同じ。Windows には Toast も切断の記録 CSV も無い) |
| `Utility/UserSetting.cs` | `CacheFolder`(既定 "") |
| `SettingsForm.cs` | Local Port の下に「Graph Cache Folder」の行(テキスト・Select・Open Folder)を**コードで**足し、区切り線・Cancel・Apply・高さを 40 下げた(`AddCacheFolderRow`。デザイナーは触っていない) |
| `MEMELib_Academic/MemeProtocol.cs` / `MEMELib.cs` | `SleepMode = 0x10`。0x83 の byte4 が 0x10 なら `memeLowBatterySleep` を出し、モードを書き換えず 0xA9 も送らない(前は Mode 16 として読み、設定を問い合わせ直していた)。出典: Gen3.0 ACP FW 仕様書 §3.6.11.5・§3.7.5(python-processing-core `docs/algorithms/firmware/notes/acp-low-voltage-sleep.md`) |
| `MainForm.AutoTest.cs` | テストの間 Graph Cache Folder を空にする(キャッシュは `<出力>\csv\logic4-cache` へ)。再生で advanced2 の「Logic4 for this file」のダイアログを `--replay-mode update|temp|cache` で閉じる(閉じないと解析が始まらず `detector replay` で時間切れになる)。replay の suite は解析後に `cache`(ファイル一覧)を result に書く |
| `MEME_Academic_Sample.Tests/CacheStoreTests.cs`(新規) | iOS の `CacheStoreTests` と同じ 7 件 |
| `README.md` / `webview/BRIDGE.md` | Graph Cache Folder・`--replay-mode`・Windows の置き場所 |

`ES_R_DevKit_Windows_Simple` は触っていない(グラフ画面が無い)。

## Windows 機でやること

1. `cd windows/ES_R_DevKit_Windows` → `dotnet build`。コンパイルの誤りがあれば直す(Mac では一度もビルドしていない)。
2. `dotnet test`。`CacheStoreTests` の 7 件が通ること(`RootFromSettings` は `Path.Combine("C:", "data")` のような形で比べている)。
3. 設定画面の見た目: `JINS_MEME_DataLogger.exe --autotest <出力> --suite settings` の `settings.png` で、Graph Cache Folder の行が Local Port の下に並び、
   Cancel / Apply と重ならないこと(高 DPI でも)。
4. 再生とキャッシュ: advanced2.zip(python-processing-core で `.venv/bin/python js/viewer/tools/make_advanced_zip.py --page advanced2`、
   `js/viewer/dist/advanced2.zip`)と、名前が `<MAC>_…` の CSV(例: `D3FE9978A82E_20261026234146.csv.gz`)で
   `--autotest <出力> --suite replay --zip <advanced2.zip> --csv <CSV> --replay-mode update`。
   - `result.json` の `cache` に `D3FE9978A82E/2026-10-27.json` があり、`.tmp` が残っていないこと
   - 出来たファイルの `logic.version` が 20261112 で、Android・Mac・iOS で同じ CSV から作ったものと窓の数・値が同じこと
     (Mac の同期フォルダ `~/Documents/ESR Logger/logic4-cache/D3FE9978A82E/2026-10-27.json` と比べる。`viewer` の欄だけは違ってよい)
   - もう一度同じ CSV を `--replay-mode cache` で開くと、計算せずに保存済みが出ること(Logic4 タブ)
5. 実機の受信: `--suite live --real --zip <advanced2.zip> --seconds 90`。止めた後、`csv\logic4-cache\<MAC>\<今日>.json` ができること。
6. 手で: 設定で Graph Cache Folder に Syncthing で同期している `logic4-cache`(または `ESR Logger`)を選び、Logic4 タブで Mac / Pixel が書いた日が見えること。
7. 電池切れ(できれば): 電池が 1/5 の MEME で計測し続け、切れたときに状態の表示が「Battery empty: …」になること
   (Pixel では SLEEP の通知の約 1 秒後に切断された。表示は 30 秒以内の切断だけ)。

終わったら、この md を消すか「済み」と書き、`webview/BRIDGE.md` の Windows の行を確かめた内容に直す。

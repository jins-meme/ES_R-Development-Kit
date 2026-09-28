# Graph page ⇄ app bridge (bridgeApi 1)

The Mac, Windows and Android apps draw their graphs in a WebView. The page inside the WebView comes from a zip
(the built-in `standard.zip`, or any zip chosen in Settings). This document is the contract between the app and
that page, so that anyone can build their own page.

## Zip layout

```text
manifest.json   {"name": "standard", "title": "Standard", "version": "1.0.0", "bridgeApi": 1, "entry": "index.html"}
index.html      the entry page (manifest.entry)
…               anything else the page needs (scripts, styles, wasm …). All paths are relative.
```

- The app refuses a zip whose `bridgeApi` differs from the one it supports (currently `1`), a zip without
  `manifest.json` or its entry page, and a zip containing symbolic links or paths leaving the zip.
- The page is served from a fixed https-like origin (Mac `memeview://app/`, Windows `https://app.memeview.example/`,
  Android `https://appassets.androidplatform.net/`), never from `file://`, so ES modules, module workers and
  WebAssembly work. Refer to your own files with relative paths, or with root paths such as `/pyodide/`
  (a relative path inside a Worker resolves from the Worker's location).
- The page must work offline. Do not load scripts from the network.

## App → page

The page defines `window.jmasHost` when it loads. The app calls it with `evaluateJavaScript` / `ExecuteScriptAsync`.
Every argument may be a value or its JSON string.

| Call | Meaning |
| :--- | :--- |
| `jmasHost.start(cond)` | A measurement starts. `cond` is below. The page clears what it showed and starts a new record. |
| `jmasHost.push(rows)` | Received samples, sent every 0.05 s: `[[i, v1, v2, …], …]`. `i` is the app's sample number (0, 1, 2 … from the start of the measurement, counting every packet). The values follow `cond.columns`, as raw LSB. |
| `jmasHost.gap()` | Reception was interrupted (the app was in the background, reconnected …). |
| `jmasHost.status(text)` | One line of status made by the app (optional). |
| `jmasHost.mark({i, text})` | A mark made in the app at sample `i` (Free Marking writes `x`). |
| `jmasHost.openReplay({url, name, timeZone, accOffset, theme})` | Replay a recorded CSV. The page fetches `url` (same origin) and owns playback: play, pause, speed, seek. |
| `jmasHost.setTheme("light" \| "dark")` | The app's appearance changed. |
| `jmasHost.stop()` | The measurement or replay ended. Keep what is shown. |

`cond` for `start`:

| Key | Example | Meaning |
| :--- | :--- | :--- |
| `label` | `"ESRG2_5"` | Device name |
| `mode` | `"full"` / `"standard"` / `"quaternion"` | Data mode set on the device |
| `cps` | `100` / `50` | Packets (rows) per second |
| `accRange` / `gyroRange` | `8` / `1000` | ± g / ± dps set on the device (to convert LSB: `value × range / 32768`) |
| `columns` | `["ACC_X", …, "EOG_V"]` | Names of the values in each pushed row. Same names as the CSV header. Full: `ACC_X ACC_Y ACC_Z GYRO_X GYRO_Y GYRO_Z EOG_L EOG_R EOG_H EOG_V`. Standard: `ACC_X ACC_Y ACC_Z EOG_L1 EOG_R1 EOG_L2 EOG_R2 EOG_H1 EOG_H2 EOG_V1 EOG_V2` (two EOG samples per packet). Quaternion: empty (nothing to draw). |
| `startedAt` | `1790000000000` | Epoch ms of sample 0 (for the time axis) |
| `timeZone` | `"local"` / `"utc"` | How to show clock times (app setting) |
| `accOffset` | `[0, 0, 0]` | Accelerometer offsets in LSB (app setting), added before conversion |
| `theme` | `"light"` / `"dark"` | App appearance |

## Page → app

| Platform | How |
| :--- | :--- |
| Mac (WKWebView) | `window.webkit.messageHandlers.jmas.postMessage(object)` |
| Windows (WebView2) | `window.chrome.webview.postMessage(object)` |
| Android (WebView) | `window.jmasNative.postMessage(JSON string)` |

| Message | Meaning |
| :--- | :--- |
| `{kind: "ready", bridgeApi, name, version}` | The page is ready. **The app holds its calls until this arrives.** |
| `{kind: "artifact", i, text}` | The user added an artifact. `i` is the sample number during a measurement, or the 0-based data row of the CSV during replay. The app writes it to the CSV's `ARTIFACT` column (when the measurement stops, or on Save Artifacts / disconnect during replay). |
| `{kind: "replay-info", mode, cps, accRange, gyroRange, rows, startedAt, warning}` | The page finished reading the replayed CSV (the app shows its conditions). |
| `{kind: "log", level, message}` | Written to the app log (errors in the page are sent here). |

## Reference implementation

`webview/common/` (shared by every page) and `webview/standard/` (the built-in page). `common/bridge.js` implements
this document; `common/dev.js` plays the app's part in a browser (`python3 webview/tools/serve.py`, then open
`http://127.0.0.1:8790/?dev`).

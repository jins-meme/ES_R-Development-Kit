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
  `manifest.json` or its entry page, and a zip that breaks the limits below.
- `manifest.json` may carry more keys (for example `build: {date, commit, …}`); the app ignores them.

### Limits

Every app checks the zip's central directory **before writing anything**, and refuses the whole zip if one entry fails.
Nothing is written outside the app's folder for pages. `webview/tools/make_bad_zips.py` makes zips that must be refused.

| What | Limit |
| :--- | :--- |
| zip file | 100 MB |
| Extracted, total / one file | 100 MB / 50 MB (sizes in the directory; extraction stops if an entry produces more than it declares) |
| Entries | 2000 |
| Compression ratio | 200× for a file over 1 MB |
| Paths | UTF-8, relative, at most 255 bytes and 16 levels. Not allowed: `/…`, `C:…`, `\`, `:`, control characters, `.` or `..` as a part, a part ending in `.` or a space, Windows device names (`con`, `nul`, `com1` …) |
| Duplicates | Refused, also when two paths differ only in upper / lower case or Unicode normalization, or when a file and a folder share a path |
| Entry kinds | Only files and folders: no symbolic links, no encryption, no ZIP64 or split zips, compression "stored" or "deflate" only. CRC-32 is checked |
| `manifest.json` | At most 64 KB. `name`, `title`, `version`: at most 64 characters, no control characters. `entry`: a path allowed above |

A new zip replaces the current one only after it has been fully extracted and checked; if anything fails, the current page stays.
- The page is served from a fixed https-like origin (Mac `memeview://app/`, Windows `https://app.memeview.example/`,
  Android `https://appassets.androidplatform.net/`), never from `file://`, so ES modules, module workers and
  WebAssembly work. Refer to your own files with relative paths, or with root paths such as `/pyodide/`
  (a relative path inside a Worker resolves from the Worker's location).
- The page must work offline, and **cannot reach the network**: the app answers every request with the
  Content-Security-Policy below, removes WebRTC before the page's scripts run, and does not navigate away from the page
  (Android opens a link the user taps in the browser; Mac does not). Only the app's own origin (and `blob:` / `data:`) can be loaded.

  ```text
  default-src 'self'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline';
  img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' blob: data:; worker-src 'self' blob:;
  media-src 'self' blob: data:; frame-src 'none'; object-src 'none'; form-action 'none'; base-uri 'none'
  ```
- To inspect the page (Safari's Web Inspector on Mac, `chrome://inspect` for Android), use a Debug build of the app. Release builds do not allow it.

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
| `{kind: "artifact", i, text}` | The user added an artifact. `text` must not start with `=` `+` `-` `@` (a spreadsheet would read it as a formula): the page refuses it at input and the app ignores it. Commas and line breaks become spaces; at most 64 characters. `i` is the sample number during a measurement, or the 0-based data row of the CSV during replay. The app writes it to the CSV's `ARTIFACT` column (when the measurement stops, or on Save Artifacts / disconnect during replay). |
| `{kind: "replay-info", mode, cps, accRange, gyroRange, rows, startedAt, warning}` | The page finished reading the replayed CSV (the app shows its conditions). |
| `{kind: "log", level, message}` | Written to the app log (errors in the page are sent here). |

## Reference implementation

`webview/common/` (shared by every page) and `webview/standard/` (the built-in page). `common/bridge.js` implements
this document; `common/dev.js` plays the app's part in a browser (`python3 webview/tools/serve.py`, then open
`http://127.0.0.1:8790/?dev`).

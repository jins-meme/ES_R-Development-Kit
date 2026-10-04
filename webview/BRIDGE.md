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
- Optional `"runInBackground": true` asks the app to keep sending samples while it is in the background
  (see [Running in the background](#running-in-the-background)).

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
| `jmasHost.gap()` | Reception was interrupted (the app was in the background without `runInBackground`, reconnected …). |
| `jmasHost.status(text)` | One line of status made by the app (optional). |
| `jmasHost.mark({i, text})` | A mark made in the app at sample `i` (Free Marking writes `X`). |
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
| `features` | `["notify", "records"]` | Extra page → app messages this app accepts (see [Detector notifications and tables](#detector-notifications-and-tables)). Missing or empty: none. Only sent for a live measurement |

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
| `{kind: "notify", tag, title, text, i}` | Show an app notification for a detector event. Only if `cond.features` has `"notify"`. See below. |
| `{kind: "table", name, columns, title}` / `{kind: "records", name, rows}` | Declare a table of detector results / add rows to it; the app saves each table as a CSV next to the data CSV. Only if `cond.features` has `"records"`. See below. |

An app ignores (logs and drops) a `kind` it does not know, so a page may send these to any app, but it should not:
check `cond.features` first.

## Detector notifications and tables

A page that runs detectors can have the app **notify** the user of an event (for example "stood up"), also with the
screen off, and can have the app **save its results as CSV** together with the data CSV. The app does not know what the
detectors are: the page decides what to notify and which columns to write, and the app only offers the two generic
receivers. Support: Android (3.1.0, versionCode 23). Mac and Windows do not offer `features` yet.

- **Live measurements only.** The app accepts these from `start` until shortly after `stop` (it waits about 1 s for the
  rows the page sends when it receives `stop`). It drops them during a replay (a replay may be analyzed again from the
  start, which would repeat every notification).
- Names (`tag`, table `name`): `^[a-z][a-z0-9_]{0,31}$`. Text: no control characters.
- A message is at most 64 KB, like every page → app message. Send rows about once a second, not per sample.

### `notify`

```json
{"kind": "notify", "tag": "posture", "title": "Stood up", "text": "score 82", "i": 123456}
```

| Key | Required | Meaning |
| :--- | :---: | :--- |
| `tag` | yes | Group of notifications. **One notification per tag**: a new one replaces the previous one (and still alerts). |
| `title` | yes | First line, at most 64 characters |
| `text` | | Second line, at most 200 characters |
| `i` | | Sample number of the event (the data CSV's `NUM`). The app shows that sample's time |

The app (Android) shows them in the notification channel "Detector events" (default importance; sound and vibration follow
the system settings), separate from the measurement's ongoing notification. Tapping one opens the app. At most one per
`tag` every 2 s (within 2 s, only the last one is shown, 2 s after the previous one), at most 16 tags per measurement.
Without the notification permission nothing is shown and the measurement goes on.

### `table` and `records`

```json
{"kind": "table", "name": "hve", "columns": ["HEIGHT_CM", "VELOCITY_CM_S"], "title": "Height / velocity (5 Hz)"}
{"kind": "records", "name": "hve", "rows": [[123460, 12.3, -0.8], [123480, 12.1, -1.0]]}
```

| Message | Key | Meaning |
| :--- | :--- | :--- |
| `table` | `name` | Table name. `disconnect` is reserved (the disconnect log). |
| | `columns` | Column names, `^[A-Z][A-Z0-9_]{0,31}$`, 1 to 32 of them, no duplicates. `NUM` and `DATE` are not allowed (the app adds them). |
| | `title` | Optional, at most 64 characters. Written in the CSV's preamble |
| `records` | `name` | A table declared in this measurement |
| | `rows` | `[[i, v1, v2, …], …]`. `i` is the row's sample number (the data CSV's `NUM`); the values follow `columns` and are numbers, strings or `null` (empty) |

- **Declare after each `start`**: a `start` forgets the declarations. When the app reloads the page during a measurement
  (it calls `start` again), declaring the same `name` with the same `columns` **continues the same file**; with other
  `columns`, that table's rows are dropped (and logged). At most 16 tables per measurement.
- Numbers are written as JSON numbers. Strings: at most 64 characters, commas and line breaks become spaces, and a string
  starting with `=` `+` `-` `@` is written empty (a spreadsheet would read it as a formula).
- Rows are written in the order they arrive. Detectors may confirm results late, so they are **not necessarily in `NUM` order**.
- A row that does not have `1 + columns` values, or whose `i` is not a non-negative integer, is dropped (and logged).

The app (Android) writes each table to `<data CSV name>_<name>.csv(.gz)` in the same folder (Downloads/ESR Logger), with the
same compression setting (for example `6EAD12345678_20261003012345_hve.csv.gz`). The file is created with its first row
(a table without rows makes no file) and appended every 100 rows, like the data CSV. The app adds `DATE`: the `DATE` of the
data CSV's row with the same `NUM` (UTC, same format), empty if that sample is older than 30 minutes or was not received.
When the measurement completes, the share sheet offers these CSV files together with the data CSV.

```text
// Detector output  : hve (Height / velocity (5 Hz))
// Page  : advanced 0.1.0 (10-03 11:01)
// Data file  : 6EAD12345678_20261003012345.csv.gz
//
//NUM,DATE,HEIGHT_CM,VELOCITY_CM_S
123460,2026/10/03 01:23:45.678,12.3,-0.8
```

A page offers these per detector, off by default: the Advanced page (python-processing-core `js/viewer/advanced/`)
has "Notify" and "CSV" check boxes in its detector settings, for the detectors that declare them (notify: sit / stand;
CSV: height / velocity at 5 Hz).

## Running in the background

When the app goes to the background (the screen turns off, another app comes to the front) while measuring, the page is
hidden: `document.visibilityState` becomes `"hidden"` and `requestAnimationFrame` stops, so nothing is drawn.

- **Android**: by default the app stops calling `push` while it is in the background and calls `gap()` when it comes back.
  A page whose `manifest.json` has `"runInBackground": true` keeps receiving `push` (every 0.05 s, as usual) and no `gap()`,
  so it can keep calculating with the screen off. The measurement itself and the CSV do not depend on this.
- **Mac / Windows**: the app always keeps calling `push`; `runInBackground` changes nothing.

For a page that calculates in the background:

- Calculate **synchronously in the `push` handler**, not in a `requestAnimationFrame` loop, a timer, or a Worker.
  On Android, about a minute after the page is hidden, the WebView stops running its timers and Workers: `push` (and
  the promise callbacks it starts) keeps running, but `setTimeout` / `setInterval` callbacks and messages to and from a
  Worker wait until the page is visible again (checked on a Pixel with WebView 149, 2026-10). A page that calculates in a
  Worker therefore falls behind with the screen off, and sends its `notify` / `records` late, all at once, when the user
  comes back. Also send `notify` / `records` from the `push` handler, not from a timer. Before the page is hidden, timers
  are only throttled (to about once a second, or less).
- Drawing resumes by itself when the page becomes visible again. Keep only a bounded history, as for a long measurement.
- The page's process can still be ended by the system (for example, out of memory). The app then reloads the page and
  calls `start` again, with `startedAt` set to the reload time; anything the page kept only in memory is lost. Store results
  you need to keep (for example in `localStorage`) as they are made.

## Reference implementation

`webview/common/` (shared by every page) and `webview/standard/` (the built-in page). `common/bridge.js` implements
this document; `common/dev.js` plays the app's part in a browser (`python3 webview/tools/serve.py`, then open
`http://127.0.0.1:8790/?dev`), including the receivers for `notify` / `table` / `records` with the same rules as the app
("App features" turns `cond.features` on and off; "Download tables" saves the tables as the app would; `window.jmasDevApp`
holds what arrived).

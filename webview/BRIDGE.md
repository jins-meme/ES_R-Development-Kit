# Graph page ⇄ app bridge (bridgeApi 1)

The Mac, Windows, Android and iOS apps draw their graphs in a WebView. The page inside the WebView comes from a zip
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
- The page is served from a fixed https-like origin (Mac and iOS `memeview://app/`, Windows `https://app.memeview.example/`,
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
| `jmasHost.openReplay({url, name, timeZone, accOffset, theme, features, device})` | Replay a recorded CSV. The page fetches `url` (same origin) and owns playback: play, pause, speed, seek. `features` / `device`: as in `cond` below (Android, iOS, Mac: `["cache"]`, and `device` from the first 12 hex digits of the CSV name when it has them). |
| `jmasHost.setTheme("light" \| "dark")` | The app's appearance changed. |
| `jmasHost.stop()` | The measurement or replay ended. Keep what is shown. |
| `jmasHost.cacheResult({id, ok, error, …})` | The answer to a `cacheList` / `cacheRead` / `cacheWrite` with the same `id` (see [Cache files](#cache-files)). `common/bridge.js` turns it into the Promise of `cacheRequest`. |

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
| `features` | `["notify", "records", "cache"]` | Extra page → app messages this app accepts (see [Detector notifications and tables](#detector-notifications-and-tables) and [Cache files](#cache-files)). Missing or empty: none. Only sent for a live measurement (`openReplay` has its own `features`) |
| `device` | `"D3FE9978A82E"` | The connected JINS MEME's address without colons, upper case (the first part of the data CSV name). The folder name of its [cache files](#cache-files). Android, iOS and Mac for now (missing when the app could not read the address) |

## Page → app

| Platform | How |
| :--- | :--- |
| Mac / iOS (WKWebView) | `window.webkit.messageHandlers.jmas.postMessage(object)` |
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
| `{kind: "cacheList" \| "cacheRead" \| "cacheWrite", id, device, name, text}` | List / read / write the page's daily cache files. Only if `features` has `"cache"`. The app answers with `jmasHost.cacheResult`. See [Cache files](#cache-files). |

An app ignores (logs and drops) a `kind` it does not know, so a page may send these to any app, but it should not:
check `cond.features` first.

## Detector notifications and tables

A page that runs detectors can have the app **notify** the user of an event (for example "stood up"), also with the
screen off, and can have the app **save its results as CSV** together with the data CSV. The app does not know what the
detectors are: the page decides what to notify and which columns to write, and the app only offers the two generic
receivers. Support: Android (3.1.0, versionCode 23), Mac (1.5.0), Windows (2.3.0).

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

- **Mac**: macOS notifications (Notification Center). The app asks for permission when it starts, and shows them even
  while it is in front. Clicking one brings the app to the front. The time shown is when it was posted (macOS does not
  let the app set it).
- **Windows**: Windows notifications (Windows App SDK). They need the **Windows App Runtime 2.5**, which the app does not
  bundle: without it nothing is shown (the measurement and the tables go on), and the first time a notification is due the
  app shows a dialog, once per run, offering to download the installer. Clicking one brings the app to the front.

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
When the measurement completes, the share sheet offers these CSV files together with the data CSV. On Mac and Windows the
tables go next to the data CSV in the save folder, with the data CSV's line endings and `DATE` format; when the "save
dialog" setting moves the data CSV, its tables move with it (same folder, same new base name). There, `i` (the app's sample
number) and the data CSV's `NUM` differ when packets are lost, and the app writes the table's `NUM` as the data CSV's `NUM`
of that sample; a row for a sample that has no row in the data CSV (the first packet) is dropped.

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

## Cache files

A page can keep **one file per day per JINS MEME** (for example the 15-second windows of an analysis and the events it found),
so that a replay or another day can be shown without computing again. The app only stores and returns text; the page decides the
content. The files are meant to be synced to a PC (for example with Syncthing), so they live next to the data CSVs, in
`<data folder>/logic4-cache/<device>/<YYYY-MM-DD>.json` (Android: `Download/ESR Logger/logic4-cache/…`; iOS: the app's `Documents/logic4-cache/…`,
shown in the Files app under On My iPhone; Mac and Windows: the "Graph Cache Folder" setting, or the Save File Path when it is empty).

Only if `cond.features` (live) or the `openReplay` argument's `features` has `"cache"`. `common/bridge.js` has
`cacheRequest(kind, data)`, which adds `id`, posts the message and resolves with the app's answer (or `{ok: false, error: "timeout"}`
after 30 s).

| Message | Answer (`jmasHost.cacheResult({id, ok, error?, …})`) |
| :--- | :--- |
| `{kind: "cacheList", id, device}` | `files: [{name, size, mtime}]` — the `YYYY-MM-DD.json` files of that device (empty when there is no folder yet). `mtime` in epoch ms |
| `{kind: "cacheRead", id, device, name}` | `text` — the whole file as UTF-8 text. `error: "not_found"` when it does not exist |
| `{kind: "cacheWrite", id, device, name, text}` | `size` (bytes written). The app writes `<name>.tmp` first and then renames it to `<name>`, so a sync tool never sees a half-written file (ignore `*.tmp` in the sync tool) |

- `device` must match `^[0-9A-F]{12}$` and `name` `^\d{4}-\d{2}-\d{2}\.json$`; anything else is answered with `error: "bad_name"`.
  The app never reads or writes outside `logic4-cache/<device>/`.
- At most **16 MB** per file (`error: "too_large"`). A `cacheWrite` message may be larger than the 64 000-character limit of other
  messages (up to about twice the file limit, for JSON escaping).
- Other errors: `"no_access"` (the app has no access to the folder, see below), `"io_error"`, `"timeout"` (from `cacheRequest`).
- The app runs the requests one at a time off the UI thread, in the order they arrive. A page writing during a live measurement
  (for example every 15 s) should send the write from `push`, like `records` (see [Running in the background](#running-in-the-background)).
- **Android:** the data CSVs are written through MediaStore, which only lets an app see the files it created itself, so files that a
  sync tool brought from a PC could not be read or replaced. The cache therefore uses a folder the user grants once in the app's
  settings ("Graph cache folder": choose `Download/ESR Logger`, or its `logic4-cache` folder), through the Storage Access Framework.
  Until then every request is answered with `"no_access"`. A rename onto an existing name would make `name (1)` there, so the app
  deletes the old `<name>` just before renaming `<name>.tmp` (for a moment there is no file of that name).
- **iOS:** the folder is in the app's own Documents, so no grant is needed and files placed there by other means (the Files app,
  a sync tool with access to it) are read and replaced as they are. The new file replaces the old one in one step.
- **Mac:** the folder is the "Graph Cache Folder" setting, or the Save File Path when that is empty; a chosen folder named
  `logic4-cache` is used as is (like Android), so a folder that a sync tool keeps can be chosen. The app is not sandboxed, so no grant is needed.
- **Windows:** like Mac: the "Graph Cache Folder" setting, or the Save File Path when that is empty (a chosen folder named `logic4-cache`
  is used as is). `<name>.tmp` is renamed onto `<name>` with `File.Move(overwrite: true)` (a replace on the same volume). No grant is needed.

`common/dev.js` imitates this in the browser (`?dev`, "App features"), keeping the files in `localStorage`
(`jmasDevCache:<device>/<name>`; a browser keeps only a few MB there).

## Running in the background

When the app goes to the background (the screen turns off, another app comes to the front) while measuring, the page is
hidden: `document.visibilityState` becomes `"hidden"` and `requestAnimationFrame` stops, so nothing is drawn.

- **Android**: by default the app stops calling `push` while it is in the background and calls `gap()` when it comes back.
  A page whose `manifest.json` has `"runInBackground": true` keeps receiving `push` (every 0.05 s, as usual) and no `gap()`,
  so it can keep calculating with the screen off. The measurement itself and the CSV do not depend on this.
- **iOS**: the same as Android (no `push` in the background unless `runInBackground`, then `gap()`). iOS may also
  suspend a hidden WebView, so a page that needs to calculate with the screen off should not rely on iOS.
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

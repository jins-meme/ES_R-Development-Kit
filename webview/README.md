# Graph page (WebView)

The Mac / Windows / Android apps show their graphs with a web page in a WebView, drawn with
[uPlot](https://github.com/leeoniya/uPlot). The page is packaged as a zip: the apps bundle `standard.zip`, and
Settings → Graph Display lets you load another zip (for example one that runs your own signal processing).

```text
webview/
├── BRIDGE.md          contract between the app and the page (bridgeApi 1)
├── common/            shared by every page
│   ├── bridge.js      window.jmasHost and messages to the app
│   ├── viewer.js      page frame: control bar, playback, bottom sheet, toast, live / replay
│   ├── charts.js      charts sharing one time axis: zoom, pan, collapse, artifacts (uPlot)
│   ├── view.js        visible time range (live / history / replay head)
│   ├── store.js       received rows as Int16 blocks (live keeps 30 min, replay keeps the whole file)
│   ├── csv.js         streaming reader for recorded .csv / .csv.gz (fflate)
│   ├── ui.css         look, light and dark
│   ├── dev.js         development panel (loaded only with ?dev in a browser)
│   └── vendor/        uPlot 1.6.32, fflate 0.8.3 (MIT)
├── standard/          the built-in page: index.html + manifest.json
└── tools/
    ├── serve.py       local server for development (same layout as the zip)
    └── make_zip.py    builds dist/<page>.zip; --install copies standard.zip into the apps
```

## Using the graph

| To | PC (mouse / trackpad) | Phone (touch) |
| :--- | :--- | :--- |
| Zoom time (all charts) | Ctrl (⌘) + wheel, or trackpad pinch · 60 / 30 / 15 / 10 s buttons | Pinch horizontally · same buttons |
| Move in time | Drag · Shift + wheel · horizontal swipe · ◀◀ ▶▶ | Drag horizontally · ◀◀ ▶▶ |
| Zoom one chart vertically | Wheel over its y axis · 縦 − / 縦 + | Pinch vertically · ↕ 縦 |
| Move vertically | Drag its y axis | — |
| Fit to the waveform | 自動 (keeps fitting until pressed again) | same |
| Reset | The chip next to the title (one chart) · 表示を戻す (everything) · key 0 | same |
| Collapse a chart | 畳む / 開く | same |
| Add an artifact | Click without dragging | Tap |
| Scroll the page | Wheel without modifiers | Vertical drag |

## Developing

```sh
python3 webview/tools/serve.py --data <folder with CSVs>
# http://127.0.0.1:8790/?dev               development panel: simulated measurement, open a CSV
# http://127.0.0.1:8790/?dev&mock=standard start a simulated Standard-mode measurement
# http://127.0.0.1:8790/?dev&replay=/data/<file>.csv.gz
python3 webview/tools/make_zip.py --install   # rebuild standard.zip and copy it into the apps
```

After changing anything in `common/` or `standard/`, run `make_zip.py --install` so the apps bundle the new zip.

On Mac, a Debug build can check the whole path by itself (mock device → measure → artifact → stop → replay),
writing snapshots and `result.json`:

```sh
MEME_AUTOTEST_DIR=/tmp/autotest Mac/ES_R_DevKit_Mac/build/…/MEME_Academic.app/Contents/MacOS/MEME_Academic -mock
```

`-mock` is required; without it the app uses real Bluetooth and would connect to a nearby device.

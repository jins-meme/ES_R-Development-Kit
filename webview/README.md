# Graph page (WebView)

The Mac / Windows / Android apps show their graphs with a web page in a WebView, drawn with
[uPlot](https://github.com/leeoniya/uPlot). The page is packaged as a zip: the apps bundle `standard.zip`, and
Display Engine lets you load other zips (for example one that runs your own signal processing).

On Mac (1.5.0 build 37 and later), Display Engine is its own dialog, opened from the app menu (below Settings…) or the
Display Engine button below Settings. It keeps several zips side by side and only one is active:

- **Standard** (the bundled zip) is the default and cannot be deleted.
- **Add zip…** checks the zip and adds it to the list without activating it. Adding the same file again does nothing;
  a zip whose manifest `name` is already in the list replaces that entry (it stays active if it was).
- Select a row to make it active (the graph view reloads). Deleting the active zip goes back to Standard.
- Nothing can be changed during measurement or replay.

Windows and Android still have a single choice under Settings → Display Engine (to be moved the same way).

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
│   ├── raw_specs.js   the raw waveform charts (EOG / accelerometer / gyroscope) used by the standard page
│   ├── ui.css         look, light and dark
│   ├── dev.js         development panel (loaded only with ?dev in a browser)
│   └── vendor/        uPlot 1.6.32, fflate 0.8.3 (MIT)
├── standard/          the built-in page: index.html + manifest.json
└── tools/
    ├── serve.py       local server for development (same layout as the zip)
    ├── make_zip.py    builds dist/<page>.zip; --install copies standard.zip into the apps
    └── make_bad_zips.py  zips the apps must refuse (limits in BRIDGE.md)
```

## Using the graph

| To | PC (mouse / trackpad) | Phone (touch) |
| :--- | :--- | :--- |
| Zoom time (all charts) | Ctrl (⌘) + wheel, or trackpad pinch · 60 / 30 / 15 / 10 s buttons | Pinch horizontally · window menu (60 / 30 / 15 / 10 s) |
| Move in time | Drag · Shift + wheel · horizontal swipe · ◀◀ ▶▶ | Drag horizontally · ◀◀ ▶▶ |
| Zoom one chart vertically | Wheel over its y axis · − / + | Pinch vertically · ⚙ (chart settings) |
| Move vertically | Drag its y axis | — |
| Fit to the waveform | Auto (keeps fitting until pressed again) | same |
| Reset | ↺ or the chip next to the title (one chart) · Reset view (everything) · key 0 | same |
| Collapse a chart | ∧ / ∨ left of the title | same |
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

Add `MEME_AUTOTEST_ZIP=<zip>` to run the same check with another page (it is loaded the way Display Engine loads it,
and the zips and the active one are restored afterwards). On Mac, `MEME_AUTOTEST_SUITE=engines` checks adding,
activating, replacing and deleting zips (`-mock`). To check that bad zips are refused:

```sh
python3 webview/tools/make_bad_zips.py /tmp/badzips
MEME_AUTOTEST_DIR=/tmp/autotest MEME_AUTOTEST_SUITE=zip MEME_AUTOTEST_BADZIPS=/tmp/badzips …/MEME_Academic -mock
```

On Windows, a Debug build has the same kind of check behind command-line arguments (no Bluetooth is used: the rows of a
recorded CSV are fed into the app's receive path at 100 Hz). See `windows/ES_R_DevKit_Windows/README.md` for all options.

```bat
JINS_MEME_DataLogger.exe --autotest C:\autotest --csv <recorded CSV> [--zip <zip>]
JINS_MEME_DataLogger.exe --autotest C:\autotest --suite zip --badzips C:\badzips
```

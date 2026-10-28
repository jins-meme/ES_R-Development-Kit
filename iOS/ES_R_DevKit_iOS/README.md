# ES_R_DevKit_iOS

> [!NOTE]
> **Delay and missing data at 100 Hz on iOS**
>
> At 100 Hz the device sends 100 packets per second, but iOS receives only about 96–97 packets per second
> (iOS keeps the Bluetooth connection interval at about 30 ms and takes about 3 packets in each interval).
> The packets that iOS has not received yet wait in the device. Right after the measurement starts the data arrives
> without delay. Then the delay grows little by little. After about 25 seconds it reaches about 0.5 seconds and stays
> there. From that point the device drops the packets that do not fit, so about 3–4 % of the samples are missing.
> These show as a success rate below 100 %. The app on the phone cannot change the connection interval.
>
> If you need real-time response or complete data, measure with Android.
> (Measured on iPhone 16 Pro with iOS 27.2.)

iPhone / iPad app for JINS MEME ES_R: scan, connect, measure, record CSV and show the graphs.
The screen follows `android/ES_R_DevKit_Android2`; the CSV, the graph page (WebView) and the artifacts work the same way
as the Mac app (`Mac/ES_R_DevKit_Mac`). Requires iOS 17 or later.

## Run

1. Open `MEME_Academic.xcodeproj`, select your Team in *Signing & Capabilities* (and change the bundle ID if needed),
   connect an iPhone / iPad with Developer Mode on, and Run.
2. **Scan device** → choose the device → **Connect** → hold **Start Measurement** (0.7 s).
3. Hold **Stop Measurement** to finish. Free Marking writes `X` in the `ARTIFACT` column of that row.

Use the **MEME_Academic Mock** scheme (launch argument `-mock`) to run without a device, also on the simulator.

## Screen

* **Connect to MEME device**: ▶ replays a logged CSV (`.csv` / `.csv.gz`) in the graph page, the display icon opens
  **Display Engine** (the zip that draws the graphs), the gear opens **Settings**.
  Holding **Disconnect** for 5 seconds while connected (not measuring) offers **shelf mode**.
* **Measure**: rows recorded, battery (0–5), success rate (from the packet counter) and communication rate.
* The rest of the screen is the graph page (`webview/`, built-in `standard.zip`; see `webview/BRIDGE.md`).

## Settings

Measurement mode / transmission speed / accelerometer and gyroscope range, auto-connect, reconnect when BLE drops
(default on), open the share sheet after a measurement, gzip (default on), graph time in local time, and **Initialize**
(resets the parameters stored in the device).

## Files

CSV files are saved in the app's Documents folder: **Files app › On My iPhone › ES_R DevKit** (or Finder when the
device is connected).

* `<address>_<UTC time>.csv.gz` (or `.csv`): the same ES_R CSV as the Mac / Android apps. `DATE` is UTC.
  With gzip, every 100 rows are appended as one gzip member, so the file is readable up to the last write even if the app stops.
* `<address>_<UTC time>_disconnect.csv`: when the connection drops while measuring, the time, the reason
  (`CONNECTION_TIMEOUT` = out of range, `REMOTE_USER_TERMINATED` = the device turned off, …) and the battery level just before.
* `<base>_<name>.csv(.gz)`: detector tables sent by the graph page (`webview/BRIDGE.md`).

## Background

Measurement continues with the app in the background or the screen locked (`bluetooth-central` background mode).
While the app is in the background the graph page receives no samples, and it is told about the gap when the app comes back
(as on Android). If the connection drops while measuring and reconnect is on, the app keeps a connection request to the same
device; iOS connects again when the device is back in range, also in the background, and the measurement restarts in a new CSV.

## Unattended run (Debug builds)

Launch arguments run scan → connect → measure → stop without touching the screen (`MEME_Academic/App/DebugAutoTest.swift`).
The result is written to `Documents/autotest_result.json`.

```
xcrun devicectl device process launch --console --terminate-existing --device <id> com.jins-jp.MEME-Academic-iOS -- \
  -autotestSeconds 60 -autotestDevice ESRG2_5 -autotestMode full -autotestRate 100 -autotestDropAt 20
```

`--` is needed before the app's arguments; without it `devicectl` reads `-autotestSeconds` as its own `-t`.

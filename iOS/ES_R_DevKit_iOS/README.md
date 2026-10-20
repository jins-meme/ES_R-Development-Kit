# ES_R_DevKit_iOS (rate probe)

Minimal iOS / iPadOS app that checks how fast JINS MEME ES_R data can be received over CoreBluetooth
(Full / Standard / Quaternion, 100 Hz / 50 Hz). It does not draw graphs or record sensor values.

## Run

1. Open `MEME_Academic.xcodeproj`, select your Team in *Signing & Capabilities*, connect an iPhone/iPad
   (Developer Mode on) and Run.
2. **Scan** → tap the device → choose Mode / Rate → **Start**.
3. Try foreground, background (home button / app switcher) and screen locked, then **Stop**.

## What it shows

* **Received / Lost**: samples counted from the 12-bit packet counter. A jump in the counter is counted as lost
  samples; long stalls use elapsed time to resolve counter wrap-around.
* **By app state**: seconds, average Hz and lost samples per state (`active`, `inactive`, `background`, `locked`).
  `locked` is reported only on devices with a passcode, about 10 s after locking.
* **Notification size / interval**: how many bytes each BLE notification carried (20 = one sample, 40 = two)
  and the time between notifications, which hints at the connection interval iOS chose.
* **ATT MTU** of the connection.

A per-second CSV (`rate_<UTC time>_<mode>_<Hz>Hz.csv`) is written to the app's Documents folder.
It can be shared from the app or copied from Finder / the Files app.

```
second,time_utc,phase,samples,lost,notifications,max_interval_ms,events
```

Seconds without any data are written as `0` rows, so gaps show up in the CSV.

## Unattended run

Launch arguments run scan → connect → measure → stop without touching the screen
(see `MEME_Academic/App/AutoTest.swift`). The result is written to `Documents/autotest_result.json`.

```
xcrun devicectl device process launch --device <id> com.jins-jp.MEME-Academic-iOS-test \
  -autotestSeconds 90 -autotestDevice ESRG2_5 -autotestMode full -autotestRate 100 -autotestBackgroundAt 30
```

## Status

Builds and passes the unit tests on the simulator. Not yet run against a real ES_R
(no iPhone could be connected to the development Mac so far).

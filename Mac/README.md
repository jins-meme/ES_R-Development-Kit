# ES_R-Development-Kit/Mac

## Summary

* "ES_R" means JINS MEME ES_R (previouslly called JINS MEME Academic pack)
* sample code included
* Checked runnning on MacOS 26 and Xcode 26
* Dongle(BLE receiver) is NOT needed for Mac
* To record the data, you need to write codes as needed.
* `ES_R_DevKit_Mac` now ships with a **Mock mode** so you can develop and run the app without an actual JINS MEME ES_R device (see "Mock Mode" section below).

## Step

### STEP1 Download and Build the xcode project

* Download ES_R-Development-kit at https://github.com/jins-meme/ES_R-Development-Kit
* go to /Mac/ES_R_DevKit_Mac or /Mac/ES_R_DevKit_Mac_Simple
* launch "MEME_Academic.xcodeproj"
* Then, build the project

When, successfully build, sample UI shows up.

* ![successbuild](https://cloud.githubusercontent.com/assets/18042520/26821411/48e4c6ce-4ae1-11e7-844b-b424ae910582.png)

### STEP2A Connect JINS MEME ES_R

* Turn JINS MEME ES_R on. Then, press "Scan MEME" then connect your JINS MEME
* NOTICE that device Number shown in UI (e.g. 1C1-1047 in the image) is not equivalent to Bluetooth ID of JINS MEME
![screen shot 2017-06-06 at 18 01 45](https://cloud.githubusercontent.com/assets/18042520/26821738/6faa99d6-4ae2-11e7-9e48-d62387ad4bf9.png)

* When your JINS MEME ES_R is connected, "Start Measurement" button shows up.
* Press "Start Measurement" button to start working JINS MEME
![screen shot 2017-06-06 at 18 02 08](https://cloud.githubusercontent.com/assets/18042520/26821755/7ef5c136-4ae2-11e7-9913-27c6ee52397d.png)

### STEP2B Using Mock Mode

**Mock mode** replaces the CoreBluetooth-backed device with an in-app mock generating simulated data. This is useful when you want to develop, test, or demo the app without owning a real JINS MEME ES_R, or without enabling Bluetooth.
If you want to use the Mock mode, select "MEME_Academic Mock" scheme on Xcode.

### STEP3 Making your original application

Enjoy!

## Architecture

The MEME library has been abstracted behind a Swift protocol so that real and mock implementations can be swapped at startup.

```
              ┌────────────────────────┐
              │     ViewController     │
              └───────────┬────────────┘
                          │ uses
                          ▼
              ┌────────────────────────┐
              │  MEMELibInterface      │  ← protocol
              └───────────┬────────────┘
                          │ conforms
            ┌─────────────┴─────────────┐
            ▼                           ▼
  ┌──────────────────┐        ┌──────────────────────┐
  │ MEMELib_Academic │        │ MockMEMELib_Academic │
  │ (CoreBluetooth)  │        │ (in-app simulation,  │
  │                  │        │  DEBUG only)         │
  └──────────────────┘        └──────────────────────┘
            ▲                           ▲
            └───────────┬───────────────┘
                        │ instantiated by
                        ▼
              ┌────────────────────────┐
              │   MEMELibFactory       │
              │  decides by launch arg │
              └────────────────────────┘
```

`MEMELibFactory.make()` returns an instance conforming to `MEMELibInterface`:

* If the process is launched with the `-mock` argument, it returns `MockMEMELib_Academic`.
* Otherwise it returns the real `MEMELib_Academic`.

### How to switch mode

The repository ships with two shared schemes:

| Scheme | Behavior |
|--------|----------|
| `MEME_Academic` | Real device via CoreBluetooth (default) |
| `MEME_Academic Mock` | In-app mock (launch argument `-mock`) |

To use mock mode:

1. Open `MEME_Academic.xcodeproj`.
2. From the scheme dropdown next to the Run / Stop buttons, select **MEME_Academic Mock**.
3. Run (`Cmd + R`). The app launches without requiring Bluetooth.

To return to the real device, switch back to the **MEME_Academic** scheme.

### What the mock does

The mock reproduces the basic Scan → Connect → Measure → Disconnect flow:

* **Scan** — After ~300 ms a simulated device named `ESR_MOCK` is reported via `memePeripheralFoundDelegate`.
* **Connect** — After ~300 ms `memePeripheralConnectedDelegate` fires with `MEMELIB_OK`. `memeVersion` is `99.0.0` and `macAddress` is `MOCK00000000`.
* **Measurement** — A timer drives data delivery at the rate selected by `setTransMode` (100 Hz for `MEMEQuality_High`, 50 Hz for `MEMEQuality_Low`). Dummy values for the selected mode are emitted:
  * `MEMEMode_Standard` → `AcademicStandardData` (acc / 4-channel EOG, with derived H and V)
  * `MEMEMode_Full` → `AcademicFullData` (acc / gyro / EOG, with derived H and V)
  * `MEMEMode_Quaternion` → `AcademicQuaternionData` (W / X / Y / Z)
  Values are generated from sine and cosine functions so charts visibly animate.
* **Disconnect** — After ~100 ms `memePeripheralDisconnectedDelegate` fires; the mock returns to the idle state and **can be scanned again** for the next session.

State transitions are logged to the Xcode console with the prefix `[Mock]`, for example:

```
[MockMEMELib_Academic] initialized
[Mock] state idle -> scanning
[Mock] emit memePeripheralFoundDelegate: ESR_MOCK
[Mock] state scanning -> connecting
[Mock] emit memePeripheralConnectedDelegate (state=connected)
[Mock] startDataReport (mode=1, interval=0.01)
[Mock] state connected -> idle
[Mock] emit memePeripheralDisconnectedDelegate
```

### Source files

| File | Role |
|------|------|
| `MEME_Academic/MEMELibInterface.swift` | Protocol that defines the public surface used by `ViewController` |
| `MEME_Academic/MEMELib_Academic.swift` | Real implementation backed by `CoreBluetooth` |
| `MEME_Academic/MockMEMELib_Academic.swift` | Mock implementation (DEBUG only) |
| `MEME_Academic/MEMELibFactory.swift` | Selects implementation based on `-mock` launch argument |
| `MEME_Academic.xcodeproj/xcshareddata/xcschemes/MEME_Academic Mock.xcscheme` | Scheme that passes `-mock` |

### Extending the mock

If you want to customize the simulated data (waveforms, battery level, device name, packet rate, etc.), edit `MockMEMELib_Academic.swift`. Key entry points:

* `tick()` — invoked on each timer fire; dispatches to one of `makeStandardData()` / `makeFullData()` / `makeQuaternionData()`.
* `sinValue(_:freq:phaseShift:)` — helper used to build oscillating Int16 channel values.
* `mockDeviceName`, `mockUUID`, `battLvMock`, `memeVersion` — constants advertised to the app.

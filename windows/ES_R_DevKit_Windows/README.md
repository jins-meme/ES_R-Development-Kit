# ES_R Development Kit for Windows (full-featured logger)

A measurement logger that handles the JINS MEME ES_R using the Windows PC's own BLE Central.
It corresponds to the Mac version `ES_R_DevKit_Mac`. If you want a minimal sample, see
[`ES_R_DevKit_Windows_Simple`](../ES_R_DevKit_Windows_Simple/README.md).

Requirements and build steps are collected in [windows/README.md](../README.md).

```
dotnet build
dotnet run --project MEME_Academic_Sample
dotnet test
```

The output is `MEME_Academic_Sample/bin/Debug/net10.0-windows10.0.22621.0/JINS_MEME_DataLogger.exe`.
Note that the project folder name (`MEME_Academic_Sample`) differs from the executable name
(`JINS_MEME_DataLogger.exe`). The exe does not work on its own, so handle the whole folder.

## Screen

The left column holds connection and measurement controls, and the graph view is on the right (the same layout as the Mac version's `ContentView`).
The graph view is a **web page inside WebView2** (the standard zip in `webview/`, or a zip chosen in Settings),
and it is the same page across the three apps (Mac / Windows / Android). The contract between the page and the app is in
[`webview/BRIDGE.md`](../../webview/BRIDGE.md).

- **Setting (S) menu** — Settings such as the save location, TCP output, and the contents of the graph view (see [Setting](#setting))
- **Version display** — App and ES_R firmware versions
- **Scan → select device → Connect** — The connection state appears in `State :`.
  While connected, holding `Disconnect` for 5 seconds moves the device to Shelf mode
  (see [Shelf mode](#shelf-mode))
- **File Replay** — Loads and plays back a recorded CSV (see [File Replay](#file-replay))
- **Select Mode / Trans Speed / Accel Range / Gyro Range** — Reads the device's current values on connection and reflects them
- **Start Measurement** — Starts and stops measurement and CSV recording
- **Free Marking** — Puts `X` in the ARTIFACT column of the next row. The position is also shown as a mark on the graph
- **Success rate / Communication** — Reception success rate and the communication rate over the last second
- **Graph view** — Display width (60 / 30 / 15 / 10 seconds), ◀◀ ▶▶ and LIVE, per-graph vertical zoom (− / + / Auto / ↺),
  and collapse / expand. Ctrl + wheel zooms time; drag / Shift + wheel pans.
  During measurement or playback, click to add an Artifact (see [Artifact](#artifact)).
  Only graphs that have data are shown (Full = EOG, acceleration, angular velocity / Standard = EOG, acceleration / Quaternion = none)

Waveforms are drawn with every sample, without decimation. This keeps the hum (50/60Hz) component
so that electrode condition can be judged by eye.

The **WebView2 runtime** is preinstalled on Windows 11, so it is not bundled. On machines without it, the graph area
shows where to get it. The page is served from inside the app as `https://app.memeview.example/`, and
**the page is not allowed to communicate outward** (a Content-Security-Policy is attached to every response, requests to other origins are refused,
WebRTC is disabled at the very start of loading, and navigation to external pages and new windows are blocked). Developer tools (F12) are available only in Debug builds.

**Detector notifications and result tables** (2.3.0): Detector events sent by the graph view page (full-featured version) are shown as Windows notifications,
and computation results are written to a CSV in the same folder as the data CSV, with the same base name + `_<name>` (the specification is in
Detector notifications and tables in `webview/BRIDGE.md`). Notifications use the **Windows App SDK runtime (Windows App Runtime 2.5)**, which is
**not bundled with the app**. On a PC without it, only the notifications are skipped (measurement and tables continue as usual), and
the first time a notification is attempted, a dialog pointing to the installer download is shown once per run (the same applies to a PC that has only
the main package installed as another app's dependency; the accompanying packages needed for notifications are installed by the official installer).

## Structure

| Project | Role |
|---|---|
| `MEMELib_Academic` | BLE connection and protocol, CSV file reading and writing (`CsvFile.cs`) |
| `MEME_Academic_Sample` | The logger itself (WinForms) |
| `MEMELib_Academic.Tests` | Unit tests for encryption, packet parsing, and CSV reading/writing |
| `MEME_Academic_Sample.Tests` | Unit tests for the logger itself (CSV header and row format, independence from the OS language, Artifact sanitization and write-back) |

Layout of `MEME_Academic_Sample`:

| File | Contents |
|---|---|
| `MainForm.cs` | Screen state transitions and operations. Corresponds to the Mac version's `MEMEViewModel` |
| `Services/WebBridge.cs` | Communication with the graph view (WebView2), serving, and communication restrictions. Corresponds to the Mac version's `WebBridge.swift` |
| `Services/WebContentStore.cs` | Extraction and switching of the graph view contents (zip). The bundled `WebContent/standard.zip` |
| `Services/ZipExtractor.cs` | Zip inspection and extraction (rejects zip slip, zip bombs, links, etc. before extracting. Rules are in Limits in `webview/BRIDGE.md`) |
| `Models/MeasurementMode.cs` | Per-mode column names/order and range table (the CSV header and rows, the columns and values passed to the graph, and the choices in the left column all refer to this same table) |
| `Services/ArtifactBuffer.cs` | Sanitization of Artifacts added in the page, and conversion to CSV data-row numbers |
| `Services/CommunicationStatsTracker.cs` | Aggregation of success rate and communication rate |
| `Services/DataPersistenceService.cs` | CSV header generation, row formatting, and buffered saving |
| `Services/TcpOutputServer.cs` | External output over TCP |
| `Services/CsvArtifactWriter.cs` | Writing Artifacts back into the CSV |
| `Services/DetectorNotifications.cs` | Shows the detector's notify events as Windows notifications (Windows App SDK; the runtime is not bundled) |
| `Services/DetectorOutputs.cs` | Receives the page's notify / table / records and writes result tables to CSV |
| `Utility/` | Helpers: file association, file error text, network info, and saving/restoring settings (`UserSetting.cs`) |
| `AppInfo.cs` / `VersionForm.cs` | App name, version and icon lookup / the version dialog |
| `SettingsForm.cs` | Setting dialog |
| `MainForm.AutoTest.cs` | Self-test for Debug builds only (see [Self-test](#self-test-debug-build)) |
| `ShelfModeForm.cs` | Confirmation dialog for Shelf mode |
| `UI/UiTheme.cs` | Corner radius and border color. Matched to the Mac version's `cornerRadius: 6` |
| `UI/RoundedButton.cs` | Rounded button. Standard buttons have square corners, so this is drawn by hand |

## File Replay

Choosing a CSV with `File Replay` starts playback immediately. **Playback is handled by the graph view (the page)**
(the app only passes the file to the page). When the page finishes reading, Select Mode / Trans Speed / Accel Range /
Gyro Range switch to the recording conditions of the file, and the file name appears in `State :`.

- Play, pause, speed, and position controls are in the control bar below the graph view.
- Rows with a value in the CSV's `ARTIFACT` column are overlaid on the graph with a vertical line and a label.
- `Disconnect` ends playback (Artifacts added are written back at that time).
- The CSV format that can be loaded is this app's own (shared with the Mac and Android versions). Both `.csv` and `.csv.gz` can be opened.
- You can also launch it by right-clicking a `.csv` / `.csv.gz` in Explorer → "Open with".

## Shelf mode

Shelf mode (storage mode) is a device-side mode that disables the pairing function to reduce power consumption.
Use it before shipping or long-term storage. **The only way to return is charging; it cannot be reverted from the app.**

While connected and not measuring, **hold `Disconnect` for 5 seconds** and a confirmation dialog appears;
choosing `Yes` performs the transition. This is a hidden operation to prevent accidents, so no gauge or similar is
shown while holding. The procedure is the same as in the Mac version and the Web Bluetooth SDK.

1. Send the transition to CONFIG mode (`ADN_SET_MODE` with mode=0x0F)
2. Wait for its ACK (`0x8F`, 3-second timeout)
3. Send the SHELF command (`0x41` + ASCII `"SHELF"`)
4. Success if the device disconnects by itself (failure if it is still connected after 5 seconds)

If no ACK arrives, SHELF is not sent, so even on failure the device stays in normal mode.

## Artifact

Clicking the graph during measurement or playback shows an input field inside the graph view. Confirming it empty
enters `X`. The mark is shown on the graph immediately, and written back to the CSV in bulk at the next opportunity.

| Situation | When it is written back | Where it is written |
|---|---|---|
| During measurement | `Stop Measurement` / disconnect | The CSV saved for that measurement |
| During playback | `Save Artifacts` / `Disconnect` | The source CSV of the playback |

- Commas and line breaks are replaced with spaces so columns do not break (up to 64 characters).
- Text starting with `=` `+` `-` `@` is not accepted (spreadsheet software would read it as a formula; the page also rejects it on input).
- If you add one to the same row multiple times, the last value entered overwrites the earlier ones.
- The `X` from `Free Marking` is written to the CSV on the spot at reception, so it is not subject to write-back.
- Write-back writes to a temporary file and then replaces the original, so the original CSV is not corrupted even if it fails midway.

The former "drag to cut out a range" feature has been removed (use Artifacts to mark ranges instead).

## Setting

Open it from `Setting (S)` in the menu bar. The contents are saved to `%APPDATA%\JINS\MEME_Academic\settings.json`
and restored on the next launch.

| Item | Details |
|---|---|
| Save File Path | CSV save location. Defaults to `Documents\JINS\MEME_Academic` |
| Acc Offset X / Y / Z | Offset (LSB) added to the graph display only. CSV values are unchanged |
| Save Format | Save measurement data gzip-compressed (default ON). ON gives `.csv.gz`, OFF gives `.csv` |
| Save Dialog | Show a dialog to re-choose the save location after measurement ends |
| Time Display | Show graph times in local time (recording is always UTC) |
| TCP Output | Stream measurement data to the outside over TCP |
| Local Port | Listening port. Defaults to 88 |
| Display Engine | Contents of the graph view. `Choose zip…` inspects the chosen zip on the spot and imports it (if it fails, the reason is shown in red and nothing changes); `Use Built-in` returns to the bundled standard version. Cannot be switched during measurement or playback. Extracted to `%LOCALAPPDATA%\JINS\MEME_Academic\WebContent` |

## TCP output

Turning `TCP Output` ON starts listening on the specified port (`Status :` in the left column becomes
`Listen`). When one client connects it becomes `Accepted`, and a header and rows in exactly the same format
as the CSV are streamed. If a client was already connected before measurement started, the header
is sent when measurement starts. Only one client is accepted at a time. If the receiver stops reading and data cannot be sent within 1 second,
that client is disconnected (measurement and CSV recording are not stopped).

```
$ ncat 127.0.0.1 88
// Data mode  : Full
// Transmission speed  : 100Hz
// Acceleration sensor's range  : 2g
// Gyroscope sensor's range  : 250dps
//
//ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V
,1,2026/08/27 04:27:10.21,-200,-3415,-2284,178,-343,763,2007,2001,6,-2004
```

## CSV

Files are written to the save location from Setting as `<MAC address>_<UTC datetime>.csv.gz` (`.csv` if Save Format in Setting
is OFF). The format is shared with the Mac and Android versions, and the columns vary by mode.

| Mode | Columns |
|---|---|
| Standard | `ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,EOG_L1,EOG_R1,EOG_L2,EOG_R2,EOG_H1,EOG_H2,EOG_V1,EOG_V2` |
| Full | `ARTIFACT,NUM,DATE,ACC_X,ACC_Y,ACC_Z,GYRO_X,GYRO_Y,GYRO_Z,EOG_L,EOG_R,EOG_H,EOG_V` |
| Quaternion | `ARTIFACT,NUM,DATE,QUATERNION_W,QUATERNION_X,QUATERNION_Y,QUATERNION_Z` |

- `DATE` is UTC. Local-time display is toggled with Time Display in Setting (display only).
- `NUM` is a monotonically increasing value accumulated from differences of the device counter. If samples are dropped, the number jumps.
- `ARTIFACT` gets `X` in the single row right after `Free Marking` is pressed.
- Data is written out every 100 rows at 100Hz, or every 50 rows at 50Hz. Opening and closing the file for each row
  would drop samples; the remainder is flushed when measurement stops.
- For `.csv.gz`, each such write becomes one gzip member, and members are concatenated into the file.
  Since gzip treats concatenated members as a single file, the result can be read as-is by `gzip -d` and by this app's File Replay.
  Unlike keeping the stream open and writing the trailer at the end, the file up to that point is always complete
  even if the app crashes or a disconnect interrupts measurement (the compression ratio worsens by a few percent in measurements).

## Status relative to the Mac version

Mac version features have been ported in stages, and by now all of them are in place.

| Feature | Status |
|---|---|
| Graph view (WebView2 + uPlot. Display width, zoom, collapse. Replaceable by zip) | Implemented |
| Communication statistics | Implemented |
| Setting dialog (save location, Acc offset, TCP output, local time display) | Implemented (`Setting (S)` in the menu bar) |
| Buffered CSV saving, save location selection, post-measurement save dialog | Implemented |
| External output over TCP socket | Implemented |
| Standard / Quaternion modes (0x98 / 0x9A) | Implemented |
| File Replay (play, pause, seek, speed change. Page side) | Implemented |
| Adding an Artifact by clicking the graph | Implemented (range cut-out removed) |
| gz-compressed CSV saving (loading `.csv` / `.csv.gz`) | Implemented |
| Shelf mode transition by long-pressing `Disconnect` | Implemented |
| Detector notifications and result tables (the page's notify / table / records) | Implemented (notifications require Windows App Runtime) |

Quaternion mode has no waveforms that can be drawn on the graph, so the graph view shows "No graph to display in this mode"
(the Mac version does the same). Standard mode has two EOG samples per packet, and the page draws both.

## Self-test (Debug build)

An entry point for verifying that the graph view works from environments where the screen cannot be operated (for example, running Windows in Parallels from a Mac).
It writes the result (`result.json`) and snapshots (PNG) to the specified folder, and the app closes itself. It is not included in Release builds.

```
JINS_MEME_DataLogger.exe --autotest <output folder> [--suite live|replay|zip|settings|webcrash|reconnect] [--csv <CSV>] [--zip <zip>]
                         [--mode full|standard] [--seconds 20] [--badzips <folder>] [--probe <JS expression | @file>]
                         [--probe-live <same>] [--expect <blinks,EMR,EML[,step events]>] [--real [--device <address or name suffix>]]
                         [--socket <port> [--socket-stall]]
```

`--probe` is evaluated in the page after each scenario, and `--probe-live` while rows are being streamed in live (for example, to measure how drawing progresses).

| suite | Contents |
|---|---|
| `live` (default) | Instead of a real device, streams the rows of `--csv` into the receive path at 100 Hz: measure → Artifact → stop → play back the saved CSV (BLE is not touched). Verifies the saved CSV's mode and numbering (NUM) gaps, the Artifact row (299), that in the full-featured version's Standard mode the detector stops and a toast appears, and that Artifacts added during playback are written back by `Save Artifacts` (row 500) and `Disconnect` (row 600). **A real device (BLE) is used only when `--real` is given** (connects to the `--device` device and measures for `--seconds`; `--csv` is not needed) |
| `replay` | Plays back a copy of `--csv` (in the full-featured version, waits for analysis to finish). Writes back Artifacts with `Save Artifacts` (row 299) and `Disconnect` (row 600). Compares detection counts with `--expect` (for golden `w-sit-jump-stairs`: `459,1169,1045,2837`) |
| `zip` | Imports each zip in `--badzips` one at a time, and checks that only those starting with `good` pass, and that rejected ones leave the current contents unchanged and write nothing outside (`webview/tools/make_bad_zips.py`) |
| `settings` | Captures the settings screen (including its form during measurement) |
| `webcrash` | While measuring with the rows of `--csv` streaming in, and while playing back that CSV, crashes the graph view's process (DevTools `Page.crash`). Verifies that measurement and playback continue on the reloaded page |
| `reconnect` | Disconnect during measurement → reconnect and verify that `Start Measurement` starts measuring, and that the CSV of the disconnected session is closed the same way as on stop (files are split, NUM does not continue, Artifacts added are written back). Uses a real device with `--real`, otherwise streams the rows of `--csv` |

`--socket` enables TCP output on that port only for the duration of the test; during `live` the test itself receives from it and checks that the header and rows received
match the saved CSV (excluding the ARTIFACT column). Adding `--socket-stall` makes the receiving side stop reading, and verifies that measurement does not stop
even when sending is blocked (the blocked client is disconnected).

`--zip` is imported before starting through the same path as Display Engine, and afterward the bundled standard version is restored (if a chosen zip was in use originally,
the one set aside is restored; the same goes for the `zip` suite). The save location is `<output folder>\csv` for the duration of the test.
If what was checked does not match, `ok` in `result.json` becomes `false` and `error` holds the reason (Artifact write-back failures also go here
without showing a dialog during the test).

Notes when running from a Mac on Windows in Parallels:

- The only Mac folders visible from the VM are Desktop, Documents, and Downloads under `Z:\` (`\\Mac\Home`). Put the files used by the test there.
- **Add `--current-user` to `prlctl exec`** (so it runs in the logged-in user's session). Without it, the app starts in a session with no screen
  and WebView2 does not launch (`The graph view could not start. … (0x800705B4)`), and the app stays open without closing.
  `dotnet` may also not be found in that session, so call it by full path: `C:\Users\<user>\.dotnet\dotnet.exe`.
- `prlctl exec` drops quotation marks in arguments, so build the arguments passed to the app inside a PowerShell script (for example, by reading them from a file).
- Write PowerShell scripts passed to `prlctl exec` in ASCII only (PowerShell 5.1 reads UTF-8 without a BOM as CP932).
- Build by copying to the VM's local disk (`C:\work\…`) with `robocopy /MIR /XD bin obj` first.

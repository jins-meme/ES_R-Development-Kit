# ES_R-Development-Kit/Windows

## Summary

* "ES_R" means JINS MEME ES_R (previously called JINS MEME Academic Pack)
* **Dongle (BLE receiver) is NOT needed** — the PC's own Bluetooth LE radio is used
* Visual Studio is not required; everything builds from the command line with the .NET SDK

This directory contains two projects, in the same relationship as the Mac versions
`ES_R_DevKit_Mac` / `ES_R_DevKit_Mac_Simple`.

| Project | Purpose |
|---|---|
| [`ES_R_DevKit_Windows`](ES_R_DevKit_Windows/README.md) | Full-featured logger. Real-time charts, communication statistics, and more |
| [`ES_R_DevKit_Windows_Simple`](ES_R_DevKit_Windows_Simple/README.md) | Minimal sample. Just connects, shows sensor values, and saves CSV |

The BLE connection handling and protocol (`MEMELib_Academic`) are structured the same way in both.
To get something running and read how it works, choose Simple; for actual measurements, choose the full-featured version.

## Requirements

| Item | Details |
|---|---|
| OS | Full-featured: Windows 11 or later (the graph view uses the WebView2 runtime, which is preinstalled on Windows 11) / Simple: Windows 10 version 1809 or later / Windows 11 |
| Hardware | A Bluetooth adapter with BLE support (built-in is fine) |
| SDK | .NET 10 SDK (`winget install Microsoft.DotNet.SDK.10`) |
| IDE | Not required. VS Code + the command line is enough |

You do not need to install the Windows SDK itself. The projects target `net10.0-windows10.0.22621.0`,
so the WinRT projection (`Microsoft.Windows.SDK.NET.Ref`) is fetched automatically from NuGet.
**Visual Studio 2019 cannot handle .NET 10.** Visual Studio 2022 is needed only if you want to use
the WinForms visual designer.

## Build and run

```
cd windows/ES_R_DevKit_Windows          # or ES_R_DevKit_Windows_Simple
dotnet build
dotnet run --project MEME_Academic_Sample
dotnet test
```

The output is `MEME_Academic_Sample/bin/Debug/net10.0-windows10.0.22621.0/JINS_MEME_DataLogger.exe`.
Note that the project folder name (`MEME_Academic_Sample`) differs from the executable name
(`JINS_MEME_DataLogger.exe`). The exe does not work on its own, so handle the whole folder.

## How to connect

1. Hold the ES_R power button for 2 seconds to enter pairing mode.
2. Press `Scan`. Found devices appear in the list in the form `ESRG2_0 (28A183055C47)`.
3. Press `Connect`. Once connected, the ES_R firmware version is shown.
4. `Start Measurement` starts acquiring sensor values and recording CSV.

The CSV format (header, column order, UTC timestamps) is shared with the Mac and Android versions.

## Protocol

| Item | Value |
|---|---|
| Service | `D6F25BD1-5B54-4360-96D8-7AA62E04C7EF` |
| Notify (device → PC) | `D6F25BD4-5B54-4360-96D8-7AA62E04C7EF` |
| Write (PC → device) | `D6F25BD2-5B54-4360-96D8-7AA62E04C7EF` |
| Packet length | Fixed 20 bytes |

The handshake after connecting runs in the order
`0xA1 GetDevInfo` → `0x81` → `0xA3 GetMode` → `0x83` → `0xA9 Get6AxisParams` → `0x89`,
and the connection is complete when `0x89` is received. During measurement,
`0x99 (AUP_REPORT_ACADEMIA2)` arrives at 100Hz.

The implementation follows the Mac version `Mac/ES_R_DevKit_Mac_Simple/MEME_Academic/BLE/MEMELib_Academic.swift` and
the Android version `android/ES_R_DevKit_Android2/core/src/main/java/com/jins_jp/meme/core/ble/`.

## Troubleshooting

- **Nothing appears on `Scan`**: Check that the ES_R is in pairing mode (hold the power button for 2 seconds)
  and that Bluetooth is ON in Windows.
- **It takes a long time to find the device**: Windows scanning listens only a small fraction of the time (about 15%, and the app cannot widen it),
  so picking up the ES_R advertisement can take more than 10 seconds. Both the full-featured and Simple versions scan for 30 seconds.
  Also, Windows keeps a connection alive for a while after disconnecting, so right after a disconnect
  it can take about 20 seconds before the ES_R becomes visible again.
- **Another app is holding it**: The ES_R can connect to only one host at a time.
  If a smartphone app or similar is connected, disconnect it.
- **Connects but no values arrive**: In Windows Settings > Bluetooth & devices, remove the ES_R once
  and scan again; this can clear the GATT cache.

## About the old sample

The old .NET Framework 4.5.2 sample, which assumed a USB BLE dongle (virtual COM port), has been
removed. It depended on the non-public NuGet package `JINSMEME_ES_R` and could not be built on its own.
If you need its contents, see the Git history.

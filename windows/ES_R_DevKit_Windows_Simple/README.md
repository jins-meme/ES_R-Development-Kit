# ES_R Development Kit for Windows 2

A sample that handles the JINS MEME ES_R (JINS MEME Academic Pack) directly through the
**Windows PC's own BLE Central**. The **USB BLE dongle (virtual COM port)** required by the old
`windows/ES_R_DevKit_Windows` is **no longer needed**.

## Requirements

| Item | Details |
|---|---|
| OS | Windows 10 version 1809 or later / Windows 11 |
| Hardware | A Bluetooth adapter with BLE support (built-in is fine) |
| SDK | .NET 10 SDK |
| IDE | Not required. Everything works from the command line without Visual Studio |

You do not need to install the Windows SDK itself. The project targets `net10.0-windows10.0.22621.0`,
so the WinRT projection (`Microsoft.Windows.SDK.NET.Ref`) is fetched automatically from NuGet.

### Installing the .NET 10 SDK

```
winget install Microsoft.DotNet.SDK.10
```

After installing, you are ready once `dotnet --list-sdks` shows 10.x in a new shell.

### Editor (optional)

- **VS Code**: Install the `ms-dotnettools.csharp` extension (or C# Dev Kit) to get
  IntelliSense and the debugger.
- **Visual Studio 2022**: Needed only if you want to use the WinForms visual designer.
  `.Designer.cs` is plain C#, so VS is not required as long as you edit it by hand.
  Note that **Visual Studio 2019 cannot handle .NET 10**.

## Build and run

```
cd windows/ES_R_DevKit_Windows_Simple
dotnet build
dotnet run --project MEME_Academic_Sample
```

The output is `MEME_Academic_Sample/bin/Debug/net10.0-windows10.0.22621.0/JINS_MEME_DataLogger.exe`.
Note that the project folder name (`MEME_Academic_Sample`) differs from the executable name
(`JINS_MEME_DataLogger.exe`). The exe does not work on its own, so handle the whole folder.

Tests (no real BLE device needed):

```
dotnet test
```

## Usage

1. Hold the ES_R power button for 2 seconds to enter pairing mode.
2. Press `Scan MEME`. Found devices appear in the list in the form `ESRG2_0 (28A183055C47)`
   (times out after 30 seconds at most. Windows scanning listens only a small fraction of the time, so it can take more than 10 seconds to find the device).
3. Press `Connect`. `Status : Connected` is shown, and the ES_R
   firmware version appears in the status bar.
4. Choose the Accelerometer / Gyroscope ranges and press `Start Measurement`.
5. Sensor values are appended to `Result/<MAC address>_<UTC datetime>.csv`.
   Pressing `Free Marking` puts `X` in the ARTIFACT column of the next row.

The CSV format (header, column order, UTC timestamps) is shared with the Mac and Android versions.

## Structure

| Project | Role |
|---|---|
| `MEMELib_Academic` | BLE connection and protocol. Replaces the old `MEMELib_Academic.dll` |
| `MEME_Academic_Sample` | WinForms sample UI |
| `MEMELib_Academic.Tests` | Unit tests for encryption and packet parsing |

Contents of `MEMELib_Academic`:

| File | Contents |
|---|---|
| `MemeProtocol.cs` | GATT UUIDs, ADN/AUP opcodes, command generation, packet parsing |
| `DecEnc.cs` | Obfuscation of 20-byte packets (everything except the first 2 bytes is transformed with a fixed key) |
| `MEMELib.cs` | BLE Central implementation using WinRT (`Windows.Devices.Bluetooth`) |
| `CsvFileWriter.cs` | CSV writer that withstands 100Hz appends |
| `MEMETypes.cs` | Public enums and `AcademicFullData` |

## Protocol

| Item | Value |
|---|---|
| Service | `D6F25BD1-5B54-4360-96D8-7AA62E04C7EF` |
| Notify (device → PC) | `D6F25BD4-5B54-4360-96D8-7AA62E04C7EF` |
| Write (PC → device) | `D6F25BD2-5B54-4360-96D8-7AA62E04C7EF` |
| Packet length | Fixed 20 bytes |

The handshake after connecting runs in the order
`0xA1 GetDevInfo` → `0x81` → `0xA3 GetMode` → `0x83` → `0xA9 Get6AxisParams` → `0x89`,
and connection completion is notified when `0x89` is received. During measurement,
`0x99 (AUP_REPORT_ACADEMIA2)` arrives at 100Hz.

The implementation follows the Mac version `Mac/ES_R_DevKit_Mac_Simple/MEME_Academic/BLE/MEMELib_Academic.swift` and
the Android version `android/ES_R_DevKit_Android2/core/src/main/java/com/jins_jp/meme/core/ble/`.

## Differences from the old version (`windows/ES_R_DevKit_Windows`)

- Removed the USB dongle and COM port UI (`Scan port` / `Open`).
- Removed the dependency on the non-public `MEMELib_Academic.dll` (NuGet `JINSMEME_ES_R`),
  and implemented equivalent functionality in C#.
- Moved from .NET Framework 4.5.2 to .NET 10, and migrated the csproj to SDK style.
- Scan results are shown with the device name, not just the MAC address.
- Sensor value labels are updated at a decimated 20Hz (the CSV records all 100Hz samples).

## Troubleshooting

- **Nothing appears on `Scan MEME`**: Check that the ES_R is in pairing mode (hold the power button for 2 seconds)
  and that Bluetooth is ON in Windows.
- **Another app is holding it**: The ES_R can connect to only one host at a time.
  If a smartphone app or similar is connected, disconnect it.
- **Connects but no values arrive**: In Windows Settings > Bluetooth & devices, remove the ES_R once
  and scan again; this can clear the GATT cache.

# AdbLab

Avalonia dogfood host for **Novolis.IO.Mobile.Android**.

## What it demos

| Action | Library surface |
|--------|-----------------|
| Refresh devices | `AndroidDebugBridge.ListDevices` (ADB protocol) |
| Device stats panel | `GetDeviceInfo().FormatReport()` — identity, build, CPU, display, battery, RAM, storage |
| Inspect package | Shell `pm path` / dumpsys (package box defaults to `com.novolis.booksmobile`) |
| Install APK… | `AndroidAppInstaller` — validate zip APK, wait for device, install `-r -g`, verify when package id is set |
| Headless `--smoke` | Protocol transport, wait, package info, APK validation failure path, sync push/pull, stats |

Transport line in the UI: `protocol · <path-to-adb.exe>` (adb hosts the server only).

## Prerequisites

1. Android SDK **platform-tools** (`ANDROID_HOME` / default `%LOCALAPPDATA%\Android\Sdk`)
2. Phone with **USB debugging** authorized (`device` state)
3. Until the package is on GitHub Packages, build with ProjectReference mode

## Run

```powershell
cd novolis-dogfooding

# UI
dotnet run --project apps/io/AdbLab -p:NovolisUseProjectReferences=true

# Headless (exit 0 when a ready device is present)
dotnet run --project apps/io/AdbLab -p:NovolisUseProjectReferences=true -- --smoke
```

ProjectRef mode is non-transitive for NuGet: the app also PackageReferences `AdvancedSharpAdbClient` explicitly.

## Tips

- Leave the package box as `com.novolis.booksmobile` when installing Books Mobile so post-install verify runs.
- Clear or change the package box for other APKs; verify is skipped if the box is empty.
- Read-only by default — install is always an explicit file-picker action.

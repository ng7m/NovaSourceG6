# Development and Operation

## Prerequisites

Windows 11, Visual Studio Community 2026 with the .NET desktop development workload, and .NET 10 SDK. `global.json` pins SDK 10.0.400. The repository-local SDK, when installed, is `.dotnet/dotnet.exe`.

## Build and test

From the repository root, use direct project commands. Substitute `dotnet` for `.\.dotnet\dotnet.exe` when using an installed SDK matching `global.json`.

```powershell
.\.dotnet\dotnet.exe restore tests\NovaSourceG6Config.Tests\NovaSourceG6Config.Tests.csproj
.\.dotnet\dotnet.exe build application\NovaSourceG6Config\NovaSourceG6Config.csproj -c Debug --no-restore
.\.dotnet\dotnet.exe test tests\NovaSourceG6Config.Tests\NovaSourceG6Config.Tests.csproj -c Debug --no-restore
.\.dotnet\dotnet.exe build application\NovaSourceG6Config\NovaSourceG6Config.csproj -c Release --no-restore
.\.dotnet\dotnet.exe test tests\NovaSourceG6Config.Tests\NovaSourceG6Config.Tests.csproj -c Release --no-restore
```

Sprint 1 recorded a workload-resolver issue with solution orchestration using the local SDK. Direct project builds provide the supported verification path; do not interpret a solution resolver failure as a passed product build.

## Operator startup

Run `application/NovaSourceG6Config/bin/Release/net8.0-windows/NovaSourceG6Config.exe`, or start the application project from Visual Studio. Keep the whole output directory together, including runtime dependencies and LICENSE. The target is .NET 8 with major roll-forward; the pinned build SDK is .NET 10. See [release packaging](../packaging/README.md) for installer/ZIP publishing and test signing.

Startup lists ports and selects the last successful port when available, but stays disconnected. Selecting a different port also stays disconnected. Click **Connect** to open the selected port, validate the G6 prompt, and read its state. This sends documented serial commands; this application is no longer a non-transmitting port probe. A successful connection stays open until disconnect or application close. The serial format is fixed at 38400 baud, 8 data bits, no parity, one stop bit, and no flow control.

For the documented bridge, the application selects COM50 and hub4com owns COM51. Never open COM51 from the application. A missing remembered port requires selecting an available port and clicking Connect; a failed connection does not replace the saved preference.

The main window saves size, normal position, monitor, DPI, and maximized state on normal close. It restores usable placement within the monitor working area, falling back to the primary monitor when necessary. Minimized closure does not cause minimized startup. Smaller windows provide scrolling so status and controls remain reachable. Windows 11 virtual-desktop membership is not saved: launch on the current desktop.

Local preferences are under `%LOCALAPPDATA%/NovaSourceG6Config/`: `connection-profile.json`, `window.json`, and `theme.txt`. Missing or malformed window settings use defaults. Preference write failures must not prevent closing.

## Recovery and sweeps

Timeouts, transport failures, and interrupted command transactions discard the serial connection. Failed configuration operations clear the confirmed UI state; reconnect to read what the instrument actually accepted. Some commands may have taken effect before a failure. Reconnection does not automatically replay failed changes.

Pause prevents subsequent sweep commands after any in-flight command finishes. Stop and closing the sweep cancel further work; an in-flight command may already have reached the instrument. Closing the sweep reads the final state when communication remains valid, or requires reconnecting when uncertain. Dwell is a delay between commands, not a guarantee of precise instrument timing. Application close waits for an active main-window operation to finish before disposing its connection.

Use the title-bar system menu (Alt+Space) for theme selection and About. The About box includes Max NG7M's creator credit, the assembly version, GPL-3.0-or-later declaration, and an offline license viewer.

## Acceptance records

[Sprint 2 acceptance checklist](sprints/sprint-2/acceptance.md) separates simulator and layout evidence from hardware and desktop checks that need an operator.

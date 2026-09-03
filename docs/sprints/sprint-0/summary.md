# Sprint 0 Summary — Foundations and Serial-Port Validation

## Status

Completed and published to the `main` branch of `ng7m/NovaSourceG6`.

## Objective

Establish a Windows 11 WPF foundation and prove that the application can discover a user-selected local serial port and safely validate access without issuing any NovaSource G6 command.

## Deliverables

- `NovaSourceG6.sln` solution targeting .NET 10.
- `NovaSourceG6Config` WPF desktop application.
- `NovaSourceG6Config.Tests` automated unit-test project.
- Project folders for application code, tests, documentation, future bridge configuration, and future application configuration.
- Development, architecture, Sprint 0 plan, and this completion summary.

## Implemented behavior

The application window is named **NovaSource G6 Config**. Its **Serial Port Connection** screen provides the following workflow:

1. Enumerate all serial ports visible to Windows, including physical and virtual COM ports.
2. Refresh the port list.
3. Select one port.
4. Open the selected port and immediately close it.
5. Report either success or an actionable failure message.

The connection test intentionally sends and reads no serial data. Before opening the port it disables DTR, RTS, and handshaking. A port can still be reserved momentarily by Windows, so it must not be tested while another application owns it.

## Architecture decisions

- **Platform:** Windows 11.
- **Desktop framework:** WPF.
- **Runtime:** .NET 10.
- **Development environment:** Visual Studio Community 2026 with the .NET desktop development workload.
- **Remote serial boundary:** serial-over-TCP remains independent from the UI. A remote instrument should appear to the application as a normal local virtual COM port, such as `COM50`.
- **Testability:** the serial API is isolated behind `ISerialPortProvider` and `ISerialPortConnection`, allowing serial behavior to be tested without hardware.

## Validation

- Debug build: succeeded with zero warnings and zero errors.
- Release build: succeeded with zero warnings and zero errors.
- Automated test suite: 4 passed, 0 failed, 0 skipped.
- Release executable:
  `application/NovaSourceG6Config/bin/Release/net10.0-windows/NovaSourceG6Config.exe`

The unit tests cover port-list cleanup and sorting, required selection, a successful open-and-dispose lifecycle, and an access-denied/in-use failure.

## Repository and commit history

Sprint 0 was pushed to the public repository at https://github.com/ng7m/NovaSourceG6 on `main`.

| Commit | Change |
| --- | --- |
| `327b6bc` | Created the WPF serial-port prototype, tests, and baseline documentation. |
| `69d93e1` | Configured .NET 10 dependencies and test-project reference. |
| `690f6a4` | Renamed the application window to NovaSource G6 Config. |
| `c1fb583` | Renamed the visible heading to Serial Port Connection. |
| `4ae3ff2` | Renamed application and test project references to NovaSourceG6Config. |
| `07d2834` | Recorded Sprint 0 completion and verification details. |

## Deferred work

- Confirm the G6 programming manual, serial settings, and command protocol.
- Select and configure the separate free/open-source serial-over-TCP virtual-COM solution.
- Define connection persistence, instrument identification, and virtual-port labeling.
- Design the operator workflow and controls for frequency, amplitude, output state, and status.
- Add a G6 protocol simulator before command/control implementation.

## Sprint 1 entry criteria

Before implementation begins, review the G6 documentation and agree on the Sprint 1 goal, acceptance criteria, architecture decisions, UI scope, and test approach.


## Related documents

- [Sprint 0 plan](plan.md)
- [Initial architecture](../../architecture/initial-architecture.md)
- [Development setup](../../development.md)

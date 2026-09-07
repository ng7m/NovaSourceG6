# Sprint 2 Implementation Status

September 4, 2026. Software scope delivered and verified with automated checks. The user explicitly deferred real-device acceptance to a later sprint. Live multi-monitor/virtual-desktop validation remains an operator verification item.

## Delivered

- About box credits Max NG7M, displays assembly version, declares GPL-3.0-or-later, and provides an embedded offline full-license viewer. The official GPLv3 text is included at the repository root and copied to build/publish output. README and project metadata declare the selected license; third-party materials retain their notices.
- Startup preselects the last successfully connected COM port and waits for Connect. Selecting a port no longer initiates connection. The remembered port is saved only after prompt validation and the initial device-state read succeed.
- Default main window is 520 by 690 WPF units. Scrolling keeps controls/status accessible at smaller sizes.
- Main-window placement saves native screen coordinates, normal size, monitor, DPI, and maximized state. Restoration scales dimensions for changed DPI and clamps to a working area, with primary-monitor fallback for unavailable/off-screen placement. A per-monitor-v2 manifest enables DPI handling. No virtual-desktop identity is saved or switched.
- Serial timeouts, transport errors, and canceled in-flight commands discard the connection. Failed operations clear confirmed UI state and request explicit reconnect; failed writes are not replayed.
- Sweeps use a testable runner; pause/stop/close prevent further work after cancellation, and dialog completion obtains fresh state or marks it unverified. Trigger/modulation failures close the dialog and invalidate the main state; dialogs cannot close halfway through applying changes. Main-window close waits for active work before disposing the connection.
- Current development, bridge verification, and architecture links replace obsolete prototype operating instructions.

## Verification

Direct Debug and Release project builds and tests use repository-local .NET SDK 10.0.400. Automated suite: 54 tests, including startup/layout, monitor placement policy, transport failure/cancellation, and sweep behavior. See the command sequence in [development instructions](../../development.md).

WPF main/About content was rendered at 100%, 150%, and 200%; the About box also has dark-color renders. Default main content and About wrapping were visually reviewed. Optional local render artifacts are in `tmp/sprint-2-ui`; the test's `NSG6_UI_ARTIFACT_DIR` setting regenerates them. Rendering does not validate native window restoration or live monitor scaling changes.

The previously recorded solution-level workload-resolver issue is not claimed fixed; direct project builds remain the verification path.

## Remaining acceptance

See [acceptance record](acceptance.md). Real-device acceptance is deferred to a later sprint by user request. Frequency and sweep limits continue to come from the device LF/HF queries; other parameter limits follow the protocol. Live monitor removal, DPI changes, minimized/maximized relaunch, Windows 11 virtual desktops, and interactive license-viewer behavior remain to be exercised. No device state was changed by this implementation run.

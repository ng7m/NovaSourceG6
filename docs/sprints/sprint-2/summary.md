# Sprint 2 — Closed

Closed September 7, 2026, at the project owner's direction. **Version 0.2.0 is the first release of NovaSource G6 Config.** Packaging and web deployment are complete. Development is waiting for user feedback on this initial release; no new sprint or additional implementation work is started.

Download page: http://www.ng7m.com/downloads/NG7M/NovaSourceG6/
Project source: https://github.com/ng7m/NovaSourceG6

The first release retains its self-signed test certificate and documented validation limitations. Closing the sprint does not represent completion of formal hardware or clean-machine installer acceptance.

## Delivered

- About box credits Max NG7M, displays assembly version, declares GPL-3.0-or-later, and provides an embedded offline full-license viewer. The official GPLv3 text is included at the repository root and copied to build/publish output. README and project metadata declare the selected license; third-party materials retain their notices.
- Startup preselects the last successfully connected COM port and waits for Connect. Selecting a port no longer initiates connection. The remembered port is saved only after prompt validation and the initial device-state read succeed.
- Default main window is 520 by 690 WPF units. Scrolling keeps controls/status accessible at smaller sizes.
- Main-window placement saves native screen coordinates, normal size, monitor, DPI, and maximized state. Restoration scales dimensions for changed DPI and clamps to a working area, with primary-monitor fallback for unavailable/off-screen placement. A per-monitor-v2 manifest enables DPI handling. No virtual-desktop identity is saved or switched.
- Serial timeouts, transport errors, and canceled in-flight commands discard the connection. Failed operations clear confirmed UI state and request explicit reconnect; failed writes are not replayed.
- Sweeps use a testable runner; pause/stop/close prevent further work after cancellation, and dialog completion obtains fresh state or marks it unverified. Trigger/modulation failures close the dialog and invalidate the main state; dialogs cannot close halfway through applying changes. Main-window close waits for active work before disposing the connection.
- Current development, bridge verification, and architecture links replace obsolete prototype operating instructions.
- RF output and PLL lock polling, animated RF status, buffered serial responses, bounded connection retries, and targeted readbacks after dialog changes.
- Friendlier errors, separate technical details, hardware overview, license text reflow, and compact dialog footers.
- .NET 8 minimum target with major runtime roll-forward; the build SDK remains .NET 10.
- Self-signed per-user Windows x64 installer, installation-free ZIP, matching source archive, release notes, and SHA-256 checksums.
- Versioned release deployment with checksum verification and a published download page linking to the installer, ZIP, GitHub project, notes, and checksums. Generated outputs and private signing material are excluded from Git.

## Verification

The final release packaging run passed the Release application build with no warnings or errors and all 59 automated tests on .NET 10. Earlier independent runs of the 59-test suite passed on .NET 8.0.30 and .NET 9.0.19. Direct project commands use repository-local SDK 10.0.400. See [development instructions](../../development.md).

The 0.2.0 installer compiled successfully. Test-signature metadata and package contents were checked. All five public release files were downloaded from the deployed HTTP site and matched local checksums. Installation on a clean machine and formal device acceptance were not performed by the agent.

WPF main/About content was rendered at 100%, 150%, and 200%; the About box also has dark-color renders. Default main content and About wrapping were visually reviewed. Optional local render artifacts are in `tmp/sprint-2-ui`; the test's `NSG6_UI_ARTIFACT_DIR` setting regenerates them. Rendering does not validate native window restoration or live monitor scaling changes.

The previously recorded solution-level workload-resolver issue is not claimed fixed; direct project builds remain the verification path.

## Follow-up after user feedback

See [acceptance record](acceptance.md). Formal real-device acceptance remains deferred by agreement. Remaining desktop and installer scenarios are follow-up items, not blockers reopening Sprint 2. User feedback will determine the next sprint's scope; trusted signing can be considered later. No automatic monitoring, feature development, or new release is scheduled.

The existing 0.2.0 download artifacts and their matching checksums remain unchanged. Their embedded release notes use the original “test release” wording. The project designates that same version as its first release. The NG7M update-link change is in source for the next build; the published 0.2.0 binary still opens GitHub Releases.

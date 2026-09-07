# Sprint 2 — Operational Reliability and Device Validation

**Status: Closed September 7, 2026.** Version 0.2.0 is the application's first release. Packaging and deployment were delivered as expanded sprint scope. See [closure summary](summary.md). Further changes await initial user feedback; the plan below is retained as the historical scope record.

## Planning status

Scope updated September 4, 2026. The user confirmed operational reliability and real-device validation as the priority and added the About box, manual startup connection, layout correction, and window-placement persistence described below. Implementation was authorized, and GPL-3.0-or-later was selected. The user explicitly deferred real-device acceptance to a later sprint. See summary.md and acceptance.md for evidence and remaining verification.

## Starting point

[Sprint 1](../sprint-1/summary.md) is closed with the configuration controls, sweeps, persistent single-device connection, and simulator-backed protocol coverage delivered. Its verification record reports 36 passing tests and successful direct project builds. It also records an established bridge connection, which does not by itself establish end-to-end device acceptance for every operator workflow.

The development guide and bridge verification procedure still contain Sprint 0 assumptions, including a non-transmitting prototype and closing the connection after validation. Sprint 1 also records a solution-level SDK workload-resolver issue.

## Objective

Make the Sprint 1 configuration utility dependable and comfortable for routine operation by strengthening simulator-backed workflows, demonstrating predictable behavior when communication fails, improving startup and window usability, declaring project authorship and licensing, and providing accurate setup and recovery instructions.

## Desired outcomes and acceptance evidence

| Desired outcome | Observable acceptance evidence |
| --- | --- |
| Deferred to a later sprint: validate supported workflows on a real device. | A dated acceptance record identifies the device and connection path and records expected versus observed results for connection, state reads, frequency, attenuation, trigger, modulation, sweep controls, and confirmed load/store operations. Any untested operation is explicitly marked unverified. |
| Communication failures leave the application in a clear, recoverable state. | Simulator scenarios cover timeout, incomplete response, device rejection, transport loss during a change, and loss during a sweep. The UI identifies failed or uncertain operations, avoids presenting unconfirmed changes as verified, and supports an explicit reconnect followed by fresh state reads. |
| Sweeps behave predictably at interruption boundaries. | Evidence covers pause, resume, stop, disconnect, and application close during a sweep. No further sweep commands are initiated after stop/disconnect completion, and the displayed final state is verified or explicitly unknown. Hardware timing observations are recorded separately from simulator results. |
| Setup and recovery instructions match the delivered application. | Development and bridge instructions describe fixed 38400/8-N-1, persistent connections, application COM50 / hub4com COM51 ownership, actual command transmission, and a reproducible build/test path. Historical architecture is clearly distinguished from current behavior. |
| The sprint can be reviewed from reproducible evidence. | Debug and Release application builds and automated tests pass; a completion summary records commands, results, hardware observations, unresolved defects, and any remaining tooling limitation. |
| The About box clearly identifies the application, creator, and license. | Show the application name and actual application version, creator credit for Max NG7M (the identity already present in the window title), and the full name of the selected GNU General Public License with its exact version and applicability. Make the license text accessible and include the matching project LICENSE file and distribution copy. Verify readability in supported themes and at increased display scaling. The selected license is GPL-3.0-or-later. |
| Startup selects the remembered port without connecting. | On launch, enumerate available ports and preselect the last successfully connected COM port when present. Remain disconnected and do not open a serial connection or transmit device commands until the operator clicks Connect. If the remembered port is absent, show an actionable disconnected state without silently connecting to a replacement. Failed connections do not replace the last successful port. |
| The initial window exposes the bottom status areas. | Correct default size and layout so the bottom text/status areas are visible on first launch and remain accessible when resized. Validate at common Windows display scales and on smaller working areas; use layout adaptation or scrolling where all content cannot fit. A saved undersized window must not reintroduce inaccessible status text. |
| Window placement survives restarts and display changes. | On normal close, save the main window's normal bounds and maximized state; on launch restore its location and size on the same available monitor. Closing minimized must not cause the next launch to be minimized. Invalid/missing settings fall back to usable defaults without preventing launch. |
| Restored placement works across monitors and Windows 11 virtual desktops. | Validate secondary monitors, negative screen coordinates, mixed DPI, monitor removal, resolution/scaling changes, and launching on another Windows 11 virtual desktop. Keep usable saved placement where possible; if off screen, place the window within the primary monitor's working area and keep the title bar and controls reachable. Launch on the current virtual desktop without forcing a switch to a previously used desktop. |

## Work order

1. Define the device acceptance checklist and expected results from the existing protocol reference; reconcile the stale setup and bridge instructions.
2. Deliver the manual-connect startup behavior, default layout fix, and robust window-placement persistence. Verify startup with remembered, missing, and unavailable ports and exercise the display/desktop scenarios above.
3. Complete the About box and matching license artifacts using GPL-3.0-or-later.
4. Review existing connection and sweep behavior against the failure scenarios above. Add focused regression coverage and fix demonstrated gaps.
5. Retain the device acceptance checklist for a later sprint; do not execute hardware checks in Sprint 2. Frequency and sweep bounds must come from device LF/HF queries.
6. Resolve acceptance defects, rerun affected checks, and publish a Sprint 2 completion summary with explicit verified/unverified results.

Physical and bridged serial paths should share the same checklist. Record results separately for each path tested; do not claim validation of an unavailable path. Real-device acceptance is explicitly deferred and is not a Sprint 2 completion gate.

## Scope boundaries

Preserve the Sprint 1 fixed serial format, single-device workflow, application/bridge separation, documented command set, and confirmations for loading or storing device settings.

This sprint excludes new instrument features, multiple-device support, named profiles, bridge management inside the application, automatic network discovery, calibration, firmware updates, and PLL reference-frequency changes. The explicitly requested usability changes above are in scope. Broad UI redesign and architectural refactoring are not objectives; make targeted changes where acceptance evidence identifies a need.

Window persistence applies to the main application window. Monitor placement and virtual-desktop membership are distinct: restoring usable geometry is required; saving or moving to a previous virtual desktop is not requested. This plan interprets the requested off-screen fallback as the primary monitor's working area.

Further UI and usability refinements are expected after these objectives are reached. Capture those requests as additional acceptance criteria when raised rather than treating unspecified future changes as current completion requirements.

Investigate the recorded SDK issue enough to document a reproducible supported build path. A full tooling repair is secondary to operator reliability unless it prevents verification.

## Planning decisions still needed

- License and creator resolved: GPL-3.0-or-later; Max NG7M.
- Real-device acceptance moved to a later sprint at the user's request.
- Frequency limits are derived from the connected device LF/HF queries; remaining parameter validation follows the documented command limits.

## Definition of done

The agreed outcomes have linked evidence, relevant automated checks pass, current operating instructions match the application, and the completion summary distinguishes delivered behavior from remaining limitations. Real-device acceptance is carried forward by agreement, not treated as unfinished Sprint 2 scope. Record remaining live desktop validation separately.

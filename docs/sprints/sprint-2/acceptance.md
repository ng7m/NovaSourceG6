# Sprint 2 Acceptance Record

Date: September 4, 2026. Software validation uses fakes and rendered WPF content. No physical G6 commands or bridge changes have been performed during this sprint implementation.

## Software evidence

- Startup/layout test: remembered COM50 is selected; raising Loaded and choosing COM3 creates no serial connection; Connect is enabled only with a selection. Missing remembered COM50 leaves the selector empty and displays an actionable message.
- WPF layout: bottom status text is within the default viewport; smaller dimensions expose horizontal/vertical scrolling. About includes assembly version and an embedded complete GPL text.
- Placement policy: negative-coordinate secondary monitor, removed monitor, changed DPI, taskbar-offset working area, corrupt settings, tiny working area, and wholly off-screen placement.
- Serial faults: silent/incomplete response, write failure, cancellation after write, and refusal to write after the connection is discarded. Existing tests cover device errors and partial application.
- Sweeps: ascending/descending/repeated points, pause/resume, cancellation while paused or in flight, and immediate stop after command failure.
- Rendered main/About content is generated at 100%, 150%, and 200% pixel scales; About is also rendered in dark colors. These renders do not substitute for real monitor DPI-transition checks.

To regenerate optional UI evidence, set `NSG6_UI_ARTIFACT_DIR` to a local output folder before running the tests. The implementation run used `tmp/sprint-2-ui`.

## Desktop acceptance — pending operator verification

| Check | Expected result | Observed |
| --- | --- | --- |
| Resize/move and close/relaunch | Same usable normal size and monitor position | Not yet tested on live desktop |
| Maximize, close, relaunch, restore | Maximized launch; normal bounds retained | Not yet tested on live desktop |
| Close while minimized | Next launch is normal or maximized, never minimized | Not yet tested on live desktop |
| Secondary monitor left/above primary | Negative coordinates restore correctly | Policy tested; native transition pending |
| Mixed 100/150/200% DPI | Usable logical size and readable layout after move/relaunch | Policy/renders tested; native transition pending |
| Remove monitor or change resolution/taskbar | Window and title bar remain within an available working area; off-screen fallback to primary | Policy tested; native transition pending |
| Launch on another Windows 11 virtual desktop | Opens on current desktop; no forced switch | Not yet tested on live desktop |
| About and full-license viewer | Creator/version/license readable in light, dark, and system themes; text selectable; close works | Content rendered/resource checked; interactive viewer pending |

## Device acceptance — deferred to a later sprint by user request

Record device model/firmware, port, physical or bridged path, test date/operator, permitted frequency/attenuation/sweep range, connected RF equipment, and initial device settings before executing. Repeat for each available connection path; do not infer physical-port results from bridged results or vice versa.

| Check | Expected result | Observed |
| --- | --- | --- |
| Launch/select port | No G6 traffic until Connect | Hardware unverified |
| Connect/read | Prompt accepted; full configuration and status agree with device | Hardware unverified |
| Frequency/attenuation | Agreed values accepted and read back; out-of-range input rejected locally | Hardware unverified |
| Trigger/modulation | Settings read back correctly; shared input changes ordered correctly | Hardware unverified |
| Sweep | Agreed bounds, direction and repeat count; pause/resume/stop; final state read or marked unverified | Hardware unverified |
| Close during sweep | Further sweep commands stop; connection disposed after work ends | Hardware unverified |
| Interrupt transport during query/change/sweep | Bounded failure; no replay; explicit reconnect yields current state | Hardware unverified |
| Load device settings | Operator confirms; saved values loaded and reread | Not authorized for test setup yet |
| Store device settings | Operator confirms; verify intended power-up settings using agreed procedure | Not authorized for test setup yet |

Record expected versus actual results and any transcript for each executed step. Restore agreed operating settings when testing finishes. Hardware acceptance is retained for a later sprint and is not a Sprint 2 completion gate. Live desktop verification remains recorded separately from automated evidence.

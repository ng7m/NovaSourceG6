# Sprint 1 Completion Summary — NovaSource G6 Configuration Utility

## Status

Closed. The implementation and clarified requirements are complete, verified, and ready to establish the Sprint 2 baseline.

## Objective

Deliver an operator-facing Windows configuration utility that connects to one NovaSource G6, reads its current configuration and status, and applies documented device commands through either a physical Windows serial port or a bridged virtual COM port.

## Clarified design baseline

- The G6 connection is fixed at 38400 baud, 8 data bits, no parity, 1 stop bit, and no flow control. Serial-format controls are intentionally absent from the UI.
- The operator selects one serial port for one G6. Named profiles and multiple simultaneous connections are not requirements.
- Bidirectional serial traffic and a persistent connection are intentional core behavior.
- For remote operation, the application uses `COM50`; com0com pairs it with `COM51`, which is owned by hub4com. Bridge installation and operation remain outside the application.
- The last successfully connected port is remembered only as a convenience and is restored when available.

## Delivered application behavior

- Natural-order enumeration of physical and virtual COM ports.
- Connection validation using the documented carriage-return transaction and G6 prompt forms.
- Persistent connect and explicit disconnect workflow with actionable errors for unavailable, busy, silent, incomplete, and unexpected endpoints.
- Device-state reads for frequency limits, frequency, attenuation, input mode, trigger mode, internal trigger, RF standby, modulation source, modulation gain, RF state, PLL lock, and power.
- Frequency entry with selectable increments and device-reported range validation.
- Attenuation adjustment across the documented 0–31 range.
- Trigger source, trigger mode, internal-trigger, and RF-standby controls.
- Modulation source and gain controls with coordination of the shared rear trigger/modulation input.
- Frequency sweeps with start, stop, step, dwell, direction, repeat, pause, resume, and stop behavior.
- Confirmed load-from-device-memory and save-to-device-memory operations.
- System-matched, light, and dark themes plus an About window and application assets.

## Protocol and bridge records

- Imported the G6 programming application note and created a normalized command reference covering framing, responses, errors, commands, status values, and safety classifications.
- Documented the separate com0com/hub4com topology, installation, active port pair, client launcher, deployment settings, and gated verification procedure.
- Verified the active client topology as `NovaSourceG6Config -> COM50 <-> COM51 -> hub4com`.
- Verified an established hub4com RFC 2217 connection from the client to `192.168.1.102:7000` during final Sprint 1 validation.

## Architecture

- `MainWindow` coordinates the operator workflow and connection state.
- `SerialPortProbeService` owns connection validation, serialized transactions, response framing, parsing, timeouts, and disposal.
- `G6DeviceService` maps protocol queries and mutations to the application device-state model.
- `ISerialPortProvider` and `ISerialPortConnection` isolate Windows serial APIs for hardware-independent tests.
- `WindowsSerialPortProvider` applies the fixed G6 serial configuration.
- `ConnectionProfileService` and `JsonConnectionProfileStore` retain the last successful port selection.
- Dedicated windows isolate sweep, trigger, and modulation workflows.

## Automated verification

The test suite contains 36 passing tests with no failures or skips. Coverage includes:

- Port cleanup and natural COM-port ordering.
- Missing, stale, busy, timed-out, and write-failure connection cases.
- Documented prompt recognition and rejection of unexpected responses.
- Command-response parsing and G6 error descriptions.
- Profile validation and persistence boundaries.
- Scripted G6 protocol transcripts, including fragmented and incomplete responses.
- Full device-state query order and state mapping.
- LED/RF/lock status validation.
- Safe command ordering and canonical frequency formatting.
- Internal-trigger, load, and nonvolatile-store commands.
- Device rejections and partial-application failure behavior.

Final direct project verification with the repository-pinned .NET SDK 10.0.400:

- Release application build: succeeded with 0 warnings and 0 errors.
- Release automated tests: 36 passed, 0 failed, 0 skipped.
- Debug automated tests: 36 passed, 0 failed, 0 skipped.

The repository-local SDK currently exhibits a workload-resolver problem when orchestrating the entire solution in one command. Direct builds of the application and test projects succeed, so this is recorded as a local SDK/tooling condition rather than a product compilation failure.

## Sprint 2 entry condition

Sprint 2 planning should start from the clarified design baseline above. New work should preserve the fixed serial format, single-device workflow, application/bridge separation, explicit confirmation for persistent device changes, and simulator-backed protocol testing.

## Related documents

- [Sprint 1 plan](plan.md)
- [Protocol command reference](../../protocol/novasource-g6-command-reference.md)
- [Initial architecture](../../architecture/initial-architecture.md)
- [Bridge overview](../../../bridge/README.md)
- [Bridge verification](../../../bridge/verification.md)
- [Development setup](../../development.md)

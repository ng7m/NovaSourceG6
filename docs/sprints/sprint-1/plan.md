# Sprint 1 — NovaSource G6 Configuration Utility

## Goal

Turn the serial-port prototype into an operator-facing NovaSource G6 configuration utility that can connect to one selected device, read its current state, and apply supported configuration changes using the documented serial protocol.

## Clarified requirements

- The NovaSource G6 serial interface is fixed at 38400 baud, 8 data bits, no parity, and 1 stop bit (38400/8-N-1), with no flow control. These settings are not operator-configurable.
- The operator selects one Windows serial port. The same workflow supports a physical port or a virtual port supplied by the separately managed hub4com bridge.
- The application intentionally reads and writes NovaSource G6 serial commands.
- A successful connection remains open until the operator disconnects, selects another port, or closes the application.
- The application remembers the last successfully connected port for convenience. It does not require named connection profiles or multiple saved devices.
- The application connects to `COM50` when using the documented bridge. hub4com owns the paired `COM51` endpoint.

## In scope

- Enumerate Windows serial ports and connect to one operator-selected port.
- Validate a connection by sending a carriage return and recognizing a documented NovaSource G6 prompt.
- Read device frequency limits, frequency, attenuation, input mode, trigger mode, internal-trigger state, RF-standby state, modulation source, modulation gain, RF state, PLL-lock state, and power state.
- Edit and apply frequency and attenuation.
- Configure trigger source, trigger mode, internal trigger, and RF standby.
- Configure modulation source and gain while respecting the shared trigger/modulation input.
- Run bounded-step frequency sweeps with pause, resume, stop, dwell, direction, and repeat controls.
- Load the device's saved configuration and store the active configuration in device nonvolatile memory after operator confirmation.
- Remember the last successfully connected serial port.
- Provide light, dark, and Windows-matched themes and application information.
- Normalize the programming application note into a repository protocol reference.
- Keep the serial-over-TCP bridge deployment separate from the operator application.
- Add automated serial framing, parsing, connection, persistence, and device-command tests using fakes and a scripted protocol simulator.

## Explicitly out of scope

- Operator-selectable baud rate, parity, data bits, or stop bits.
- Multiple simultaneous device connections or named connection profiles.
- Installing, configuring, or controlling hub4com from the application.
- Automatic discovery of a G6 across the network.
- Calibration, firmware updates, or undocumented device commands.
- Changing the PLL reference-frequency setting.

## Safety behavior

- Emit only documented commands terminated by carriage return.
- Serialize command transactions so only one request is active at a time.
- Use bounded read and write timeouts and retain raw responses for parsing and diagnostics.
- Validate frequency against the limits queried from the connected device.
- Validate attenuation, modulation gain, sweep step, dwell, and repeat inputs before transmitting changes.
- Coordinate trigger and modulation changes because they share the rear input.
- Require explicit confirmation before loading saved settings or writing active settings to nonvolatile memory.
- Disconnect and dispose the serial connection when the operator disconnects or closes the application.

## Acceptance criteria

- The application lists currently available serial ports and connects to one selected port using fixed 38400/8-N-1 settings with no flow control.
- It rejects missing, unavailable, busy, silent, incomplete, and non-G6 connections with actionable status messages.
- After connecting, it reads and displays the supported device configuration and status.
- The operator can apply supported frequency, attenuation, trigger, and modulation changes.
- The operator can run, pause, resume, and stop a validated frequency sweep.
- Loading and nonvolatile storage require explicit operator confirmation.
- The last successfully connected port is restored when it is still available.
- Physical and bridged virtual serial ports use the same application workflow.
- Protocol framing, response parsing, device-state mapping, command ordering, device errors, fragmented responses, incomplete responses, and partial failures have automated coverage.
- Debug and Release project builds complete without warnings or errors, and all automated tests pass.

## Protocol source

Serial behavior and commands are based on *Programming the NovaSource G6*, application note NL-NS002-020315, revision 2.0 (2008). The normalized reference is [`../../protocol/novasource-g6-command-reference.md`](../../protocol/novasource-g6-command-reference.md).

# Sprint 0 — Foundations and Serial-Port Probe

## Goal

Establish the project baseline and validate that a Windows 11 desktop application can enumerate serial ports and safely check that a user-selected port can be opened.

## In scope

- WPF prototype targeting .NET 10.
- Enumeration of locally visible physical and virtual COM ports.
- User-selected open/close probe with clear status feedback.
- Automated unit tests for the probe behavior.
- Base project, development, and architecture documentation.

## Explicitly out of scope

- Sending or receiving G6 commands.
- Changing frequency, output state, or any instrument setting.
- Implementing the remote serial-over-TCP bridge.
- Persisting connection settings.

## Safety behavior

The probe validates that the selected port still exists, opens it, and disposes it immediately. It never calls a read or write operation. DTR and RTS are disabled and hardware/software handshaking is disabled before opening the port.

Opening a serial port can still reserve it briefly and may cause device-driver-level state changes. Run the probe only when no other software owns the instrument port.

## Acceptance criteria

- The user can refresh and select an available COM port.
- The prototype reports success after a port can be opened and closed.
- A port in use produces an actionable error.
- The probe does not transmit instrument data.
- Automated tests pass.

## Findings and next decision checkpoint

Sprint 1 planning will use the prototype outcome to decide the connection-management model, operator workflow, and how virtual ports are labeled in the application.

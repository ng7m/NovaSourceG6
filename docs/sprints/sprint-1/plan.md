# Sprint 1 — Connection Profiles and Operator Workflow

## Goal

Turn the Sprint 0 serial-port diagnostic into a repeatable connection setup workflow while preserving the no-command safety boundary until the NovaSource G6 programming manual is reviewed.

## In scope

- Save one local connection profile containing an operator-friendly instrument label, COM port, and explicit serial settings.
- Restore the saved profile on application startup.
- Clearly report when a saved physical or virtual COM port is unavailable.
- Keep the existing non-invasive open/close test available.
- Unit-test profile validation and persistence boundaries without hardware.

## Explicitly out of scope

- Sending or reading G6 protocol data.
- Connecting persistently to the instrument.
- Frequency, amplitude, output, or status controls.
- Installing or managing a serial-over-TCP bridge.
- Assuming undocumented G6 serial settings are correct.

## Acceptance criteria

- An operator can select a visible COM port, add a friendly label, select a supported baud rate, and save the profile.
- The profile is restored after restarting the application.
- A profile whose COM port is absent remains visible and produces a clear status message.
- Saving without a port or with an unsupported baud rate fails with an actionable message.
- The application still sends and reads no serial data.
- Debug and Release builds pass with zero warnings, and all automated tests pass.

## Protocol readiness checkpoint

Before adding persistent connections or instrument commands, obtain and review the exact G6 model's programming manual. Record serial settings, terminators, command and response grammar, error behavior, and safe identification/status queries. Add a protocol simulator and transcript-based tests before hardware command testing.

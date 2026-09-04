# NovaSource G6 Config

Windows 11 control software for the NovaSource G6 programmable frequency source.

## Application status

`NovaSourceG6Config` is a WPF configuration utility for the NovaSource G6. It connects to one operator-selected serial port using the device's fixed 38400 baud, 8-data-bit, no-parity, 1-stop-bit configuration. The application reads the current device state and provides controls for frequency, attenuation, modulation, triggering, frequency sweeps, and loading or storing device settings.

The project targets .NET 10 and is intended for Visual Studio Community 2026 on Windows 11.

## Layout

- `application/` — the WPF operator application.
- `tests/` — automated tests.
- `docs/` — sprint documentation, design records, and development instructions.
- `bridge/` — reserved for separately deployed serial-over-TCP bridge configuration.
- `config/` — reserved for saved connection and instrument configuration.

See the [development setup](docs/development.md) to build and test. Historical planning records are available under `docs/sprints/`.

# NovaSource G6 Control

Windows 11 control software for the NovaSource G6 programmable frequency source.

## Sprint 0 status

Sprint 0 contains the `NovaSourceG6Config` WPF serial-port diagnostic prototype. It lists the serial ports visible to Windows and performs a non-invasive open/close check on a selected port. It sends no data to the instrument.

The project targets .NET 10 and is intended for Visual Studio Community 2026 on Windows 11.

## Layout

- `application/` — the WPF operator application.
- `tests/` — automated tests.
- `docs/` — sprint documentation, design records, and development instructions.
- `bridge/` — reserved for separately deployed serial-over-TCP bridge configuration.
- `config/` — reserved for saved connection and instrument configuration.

See the [Sprint 0 summary](docs/sprints/sprint-0/summary.md), [Sprint 0 plan](docs/sprints/sprint-0/plan.md), and [development setup](docs/development.md) to build and test.

# NovaSource G6 Config

Windows 11 control software for the NovaSource G6 programmable frequency source.

## Application status

**First release: 0.2.0. Sprint 2 closed September 7, 2026.** The application is
available for initial user feedback. Additional changes and the next sprint's
scope are on hold until that feedback is received. See the
[Sprint 2 closure summary](docs/sprints/sprint-2/summary.md).

`NovaSourceG6Config` is a WPF configuration utility for the NovaSource G6. It connects to one operator-selected serial port using the device's fixed 38400 baud, 8-data-bit, no-parity, 1-stop-bit configuration. The application reads the current device state and provides controls for frequency, attenuation, modulation, triggering, frequency sweeps, and loading or storing device settings.

The project targets .NET 8 with major-version roll-forward and is intended for Visual Studio Community 2026 on Windows 11. It can use an installed .NET 8, 9 or 10 Desktop Runtime; the build SDK remains pinned separately in `global.json`.

## Downloads and packaging

The release workflow produces a Windows x64 per-user installer and an installation-free ZIP, plus matching source and checksums. Early test packages use a self-signed certificate, which is not automatically trusted by Windows. See [packaging instructions](packaging/README.md). Release uploads are a separate explicit step. **Check for updates…** in the next build opens the [NG7M download page](http://www.ng7m.com:18080/); the already-packaged 0.2.0 build opens GitHub Releases. Browse the [project on GitHub](https://github.com/ng7m/NovaSourceG6) or download the matching source archive alongside each release. The port 18080 site requires the IIS and router cutover described in the packaging instructions.

## Layout

- `application/` — the WPF operator application.
- `tests/` — automated tests.
- `docs/` — sprint documentation, design records, and development instructions.
- `bridge/` — reserved for separately deployed serial-over-TCP bridge configuration.
- `config/` — reserved for saved connection and instrument configuration.

See the [development setup](docs/development.md) to build and test. Historical planning records are available under `docs/sprints/`.

Startup preselects the last successfully connected port and waits for **Connect**. The main window remembers its size and monitor placement. The title-bar system menu includes **About NovaSource G6 Config…**, with creator, version, and the full license.

## License and creator

Created by Max NG7M. This project's original software is licensed under the GNU General Public License, version 3 or (at your option) any later version (GPL-3.0-or-later). See [LICENSE](LICENSE). The software is provided without warranty, as described in that license. Third-party dependencies and the manufacturer application note retain their respective licenses and notices.

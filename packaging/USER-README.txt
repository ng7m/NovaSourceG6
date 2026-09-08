NovaSource G6 Config - Max NG7M
Downloads: http://www.ng7m.com:18080/
Project source: https://github.com/ng7m/NovaSourceG6

Windows 11, 64-bit x64 application. Requires .NET Desktop Runtime 8 or newer.
The app prefers .NET 8 and rolls forward to a newer major runtime if needed.
The SDK and the ASP.NET runtime are not required.
Runtime download: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

INSTALLER: Run Setup.exe. Installs for your current Windows user, adds a Start
menu shortcut, and supports removal from Windows Settings > Installed apps.
If a compatible runtime is missing, Setup offers the Microsoft download page.
Run the Desktop Runtime x64 installer, then return to Setup and try again.
The Microsoft runtime installer may require administrator permission.

ZIP: Extract the entire folder, then run NovaSourceG6Config.exe. Keep all DLLs
and other files together. This is installation-free, not fully portable:
preferences are still saved under %LOCALAPPDATA%\NovaSourceG6Config.

UPDATES: About > Check for updates opens the NG7M download page. Close G6
Config before installing an update or replacing ZIP files. Settings are
preserved during upgrades and uninstall. No automatic updates or restarts.
Do not install or replace files while a sweep or serial command is running.

TEST SIGNATURE: This release uses a self-signed test certificate. It is not
publicly trusted and does not remove Windows publisher/SmartScreen warnings.
The public .cer is provided for signature inspection; do not import it into
Trusted Root Certification Authorities. No private key is distributed.

Connect the serial adapter, select the correct COM port, and click Connect.
No connection is made automatically. USB/serial drivers, hub4com, com0com and
other bridge components are separate installations and are not bundled.

Licensed under GPL-3.0-or-later; see LICENSE. Obtain the matching source ZIP
beside this release. Third-party libraries and manufacturer material retain
their own notices. This utility is provided without warranty; verify device
settings and equipment suitability before use.

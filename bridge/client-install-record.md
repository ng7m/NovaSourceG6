# Client Bridge Installation Record

Date: 2026-09-03

## Computer

- Operating system: Windows 11 Home 64-bit, build 26200
- Existing COM ports before installation: COM3, COM4, COM5, COM6, COM105, COM106
- Requested virtual pair: COM50 to CNCB0

## Packages

| Component | Package | SHA-256 |
| --- | --- | --- |
| com0com | `com0com-3.0.0.0-i386-and-x64-signed.zip` | `6E5D4359865277430D4AE88C73FB7E648A0ED8E81AEA5002478179CFCB0BB0E1` |
| hub4com | `hub4com-2.1.0.0-386.zip` | `24CCA36CCF0CAB0F988BB59851B5EC947667EFE53C4F43F290392AD308AC0E01` |

Both packages were downloaded from the official com0com SourceForge project.

## Installation result

- com0com 3.0.0.0 is registered under `C:\Program Files (x86)\com0com`.
- The outer x64 installer has a valid Authenticode signature from CyberCircuits.
- Windows created `com0com - bus for serial port pair emulator 0 (COM50 <-> CNCB0)`.
- The device does not start on this Windows build. Device Manager reports `CM_PROB_UNSIGNED_DRIVER`, so COM50 is not enumerated by `System.IO.Ports.SerialPort` and must not be treated as usable.
- Driver-signature enforcement and Secure Boot were not disabled.
- hub4com 2.1.0.0 portable binaries are installed under `C:\Program Files (x86)\hub4com`.
- `hub4com.exe --help` runs successfully.

## Required next decision

Select a currently signed Windows 11-compatible virtual COM driver, or run the application client on a Windows environment that legitimately supports the com0com driver. Do not work around the failure by enabling test-signing mode or disabling integrity protections.

The RFC 2217 server side may still use hub4com independently, but the current client cannot expose COM50 until a compatible virtual COM driver is chosen.

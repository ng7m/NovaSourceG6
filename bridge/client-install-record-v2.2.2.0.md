# Client Bridge Installation Record — com0com 2.2.2.0

Date: 2026-09-03

## Result

- Removed com0com 3.0.0.0 and its non-starting device.
- Installed the official signed x64 com0com 2.2.2.0 package.
- Package: `com0com-2.2.2.0-x64-fre-signed.zip`
- SHA-256: `3BC7313E8577774CE78CFF4D1C816FAF96CFA48C485BBCC5527D4569F7D5E373`
- Installed driver file version: 2.2.2.0.
- The individual `.sys` file does not carry an embedded signature; the installed driver catalog validates successfully.
- Created `COM50 <-> CNCB0` with baud-rate emulation enabled on COM50.
- Windows reports the bus and both port devices as `OK` with `CM_PROB_NONE`.
- `System.IO.Ports.SerialPort.GetPortNames()` enumerates COM50 and CNCB0.
- Secure Boot, test-signing mode, and driver-signature enforcement were not changed.

## Loopback verification

The deployed topology was tested locally:

1. hub4com opened `\\.\CNCB0`.
2. .NET `System.IO.Ports.SerialPort` opened `COM50`.
3. `.NET -> COM50 -> com0com -> CNCB0 -> hub4com echo -> CNCB0 -> com0com -> COM50` returned `NOVASOURCE_BRIDGE_TEST` exactly.

hub4com defaults to CTS output handshaking. The test required this option before the CNCB0 port argument:

```text
--octs=off
```

Do not assume that this setting is correct for the NovaSource G6. Select CTS/RTS behavior from the G6 programming manual when configuring the real RFC 2217 connection.

## Installed paths

- com0com: `C:\Program Files (x86)\com0com`
- hub4com: `C:\Program Files (x86)\hub4com`

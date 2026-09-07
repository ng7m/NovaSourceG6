# NovaSource G6 Remote Serial Bridge

This directory documents the separately deployed serial-over-TCP bridge. The C# application does not manage the bridge; it sees the client-side virtual port as an ordinary Windows COM port.

## Target topology

```text
NovaSourceG6Config
  -> client COM50
  -> com0com pair (COM50 <-> COM51)
  -> hub4com RFC 2217 client
  -> private/VPN TCP connection
  -> hub4com RFC 2217 server on bridge host
  -> server physical COM port
  -> NovaSource G6
```

Use `COM50` only if it is free. The physical port and TCP port are deployment values and must not be guessed.

## Documents

- [Installation and configuration](installation.md)
- [Verification and troubleshooting](verification.md)
- [Deployment settings template](bridge-settings.example.md)

## Safety and security boundary

- Complete bridge loopback and transport tests before attaching the G6.
- Use the reviewed G6 protocol reference and the current acceptance checklist for instrument testing.
- Bind or firewall the listener to a trusted management network or VPN. RFC 2217/Telnet does not provide confidentiality or authentication by itself.
- Permit only the intended client IP to reach the selected TCP port.
- Ensure only one client owns the physical serial port.
- The application owns COM50 after Connect; hub4com owns COM51. Do not open another application on either owned endpoint.

## Source projects

com0com is the Windows virtual null-modem driver. hub4com is the companion serial/TCP redirector and includes the `com2tcp-rfc2217.bat` wrappers used here. Obtain both from the project's official SourceForge files rather than third-party download sites:

- https://sourceforge.net/projects/com0com/files/com0com/
- https://sourceforge.net/projects/com0com/files/hub4com/

The upstream packages are old. Validate them in the target Windows build and retain the exact package names and hashes in the deployment record before production use.

# Active Client Port Configuration

This document supersedes earlier `CNCB0` examples in the bridge runbook.

## Active pair

```text
NovaSourceG6Config -> COM50 <-> COM51 -> hub4com
```

- The C# application opens COM50.
- hub4com opens COM51.
- Both com0com endpoints are healthy and enumerate as standard COM ports.
- The pair was verified with a local round-trip payload.

## Client launcher

The installed launcher defaults hub4com to COM51:

```powershell
& 'C:\Program Files (x86)\hub4com\novasource-hub4com-client.cmd' SERVER_ADDRESS TCP_PORT
```

The source-controlled copy is `bridge/novasource-hub4com-client.cmd`.

The remote server address and TCP port are intentionally not hard-coded because they have not yet been provided. The launcher passes them to the installed RFC 2217 wrapper while fixing the local endpoint at `\\.\COM51`.

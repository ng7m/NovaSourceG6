# Installation and Configuration

These instructions assume Windows 11 for the application client and Windows for the bridge/server host. Run installers and elevated setup consoles only from an administrator account authorized for these computers.

## 1. Record deployment values

Copy `bridge-settings.example.md` and fill in hostnames, addresses, unused COM numbers, the physical serial port, and an unused TCP port. Leave all G6 protocol settings marked `FROM_MANUAL` until its documentation is reviewed.

Check the proposed client port before installation:

```powershell
[System.IO.Ports.SerialPort]::GetPortNames() | Sort-Object
```

If `COM50` already exists, select another unused high-numbered COM port and use it consistently below.

## 2. Download and verify packages

Download the signed x64 com0com package and hub4com from the official SourceForge project. Do not select an unsigned driver build on Windows 11.

Record each downloaded filename and hash:

```powershell
Get-FileHash -Algorithm SHA256 .\com0com*.zip
Get-FileHash -Algorithm SHA256 .\hub4com*.zip
```

Keep the archives and hashes with the deployment record. Inspect the driver signature in the file properties before installing. Stop if Windows reports an invalid, revoked, or untrusted signature; do not disable Secure Boot or driver-signature enforcement as a workaround.

## 3. Install the client virtual COM pair

1. Install the signed x64 com0com package on the application computer.
2. Open the installed **Setup Command Prompt** as administrator.
3. Run `list` and record the current pairs.
4. Create a pair whose application-facing endpoint is the chosen COM port and whose bridge-facing endpoint retains a com0com bus name. For a new pair numbered `0`:

   ```text
   install 0 PortName=COM50,EmuBR=yes -
   ```

5. Run `list` again. Confirm the pair reports `COM50` and `CNCB0` (the actual `CNCB` suffix follows the pair number).
6. Confirm Device Manager shows the virtual ports without a warning icon.

Do not use `change` or `remove` against an existing pair unless its identity and consumers have been verified.

## 4. Install hub4com on both computers

Extract hub4com into a stable, administrator-controlled directory on both the client and bridge host. Do not run it permanently from Downloads or a temporary folder.

Before attaching the G6, display the installed wrapper usage and retain it with the deployment record because package versions can differ:

```powershell
.\com2tcp-rfc2217.bat --help
```

## 5. Configure the bridge/server listener

Identify the physical serial port on the bridge host through Device Manager and the port list. With the G6 still disconnected, start an RFC 2217 listener using the physical port and chosen TCP port:

```powershell
.\com2tcp-rfc2217.bat COM_PHYSICAL 7000
```

The upstream wrapper syntax above creates the TCP-to-COM server. Do not add baud, parity, flow-control, or modem-line options until they are confirmed from the G6 manual and the installed wrapper's help.

Create a Windows Defender Firewall inbound rule scoped to TCP `7000`, the trusted/private profile, and the single client IP. Prefer a host firewall plus VPN. Never expose this listener directly to the public Internet.

## 6. Configure the client redirector

On the client, connect the hidden com0com peer to the server:

```powershell
.\com2tcp-rfc2217.bat CNCB0 BRIDGE_ADDRESS 7000
```

The C# application opens `COM50`; hub4com owns `CNCB0`. Do not point both programs at the same endpoint.

## 7. Startup automation

First validate the bridge interactively. After validation, use Task Scheduler on each host with:

- **Run whether user is logged on or not**
- **Run with highest privileges** only if required for serial access
- Trigger **At startup**, with a short delay so networking and devices are ready
- Working directory set to the fixed hub4com directory
- Automatic restart on failure
- A dedicated least-privilege service account where practical

Store the exact command, account, working directory, firewall rule, and recovery settings in the deployment record. Do not embed passwords or VPN secrets in batch files or this repository.

## 8. Removal and rollback

1. Stop the client and server hub4com processes or scheduled tasks.
2. Remove or disable the narrowly scoped firewall rule.
3. From the com0com Setup Command Prompt, run `list`, identify the exact pair, and remove only that pair using the installed version's documented command.
4. Uninstall com0com through Windows Installed Apps if it is no longer needed.
5. Reboot if the driver installer requests it, then confirm Device Manager is clean.

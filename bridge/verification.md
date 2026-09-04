# Verification and Troubleshooting

Verification is deliberately split into gates. Stop at the first failure.

## Gate A: client driver

1. `COM50` appears in Device Manager without a warning icon.
2. The com0com Setup Command Prompt `list` output shows `COM50 <-> CNCB0`.
3. The application lists `COM50` after **Refresh**.
4. With hub4com stopped, the application's non-invasive open/close test succeeds.

This test opens and closes only `COM50`; it sends no bytes.

## Gate B: network path

With the server listener running, test reachability from the client:

```powershell
Test-NetConnection BRIDGE_ADDRESS -Port 7000
```

`TcpTestSucceeded` must be `True`. If not, check listener state, DNS/routing, VPN state, and the narrowly scoped firewall rule. Do not broaden the firewall to all sources as a diagnostic shortcut.

## Gate C: bridge ownership

1. Start the server wrapper and confirm it owns `COM_PHYSICAL` without an access-denied error.
2. Start the client wrapper and confirm it owns `CNCB0` and connects to the server.
3. Confirm reconnect behavior by restarting only the server wrapper.
4. Confirm a second client is rejected or otherwise cannot concurrently control the G6 port.

Once hub4com owns `CNCB0`, do not run the application's port test against `CNCB0`. The application belongs on `COM50`.

## Gate D: transport test without the G6

Use a serial loopback plug or a second known-safe serial test endpoint on the bridge host. Send a unique non-secret test payload from a serial terminal on `COM50` and verify the exact bytes return. Test both directions and include the expected G6 baud rate only after it is known.

Do not perform an arbitrary echo test with the G6 attached: unknown bytes or line endings could be commands.

## Gate E: G6 documentation review

Provide the exact model/revision programming manual. The review must produce:

- Electrical/interface assumptions and cable requirements
- Baud, data bits, parity, stop bits, and flow control
- Required command and response terminators
- Startup banner or documented command prompt, including exact bytes
- Echo behavior, timing, timeouts, and maximum command length
- A read-only or lowest-risk identification/status transaction
- Error responses and recovery behavior
- Any command that can alter frequency, amplitude, output state, calibration, or persistent memory

No G6 traffic is authorized before this gate is complete.

## Gate F: application prompt validation

After the documentation review, update the application so serial settings are explicit and validated. The first hardware implementation should:

1. Open the saved virtual COM port asynchronously with bounded read/write timeouts.
2. Apply the documented serial settings before opening.
3. Avoid asserting DTR, RTS, or break unless required by the manual.
4. Read only the documented startup prompt if it is emitted spontaneously; otherwise send only the approved read-only query.
5. compare raw received bytes with the documented prompt/response while tolerating only documented variations.
6. Close and dispose the port on success, timeout, cancellation, or error.
7. Log timestamps and escaped byte values, excluding secrets and sensitive network data.

Add a protocol simulator and transcript-based automated tests before connecting to the real instrument.

## Common failures

| Symptom | Check |
| --- | --- |
| Virtual COM port absent | Driver signature, Device Manager status, com0com `list`, reboot requirement |
| TCP test fails | Server process, address/port, VPN/routing, scoped firewall rule |
| Physical COM access denied | Another program or stale hub4com process owns the port |
| Client COM access denied | Application and another terminal both opened `COM50`, or hub4com was pointed at the wrong endpoint |
| Connects but no bytes flow | Wrong pair endpoint, cabling, serial settings, flow control, or RFC 2217 negotiation |
| Intermittent modem-line behavior | Confirm the G6 flow-control requirements; use RFC 2217 and the installed hub4com trace/help rather than raw TCP assumptions |

# Verification and Troubleshooting

The application and bridge remain separate. The Sprint 1 deployment record uses `COM50 <-> COM51`, with the application on COM50 and hub4com on COM51. Do not run competing applications on either owned endpoint.

## Client and network checks

1. Confirm COM50 and COM51 appear without driver warnings and the com0com configuration pairs them.
2. Start the separately configured bridge server/client; confirm hub4com owns COM51 and connects to the configured listener.
3. Verify reachability with `Test-NetConnection BRIDGE_ADDRESS -Port 7000`, substituting the deployment's actual endpoint. TCP reachability alone does not prove a working G6 protocol path.
4. Confirm the application lists COM50 after Refresh. Startup and port selection do not transmit. Click Connect when ready to validate the attached G6 and read its state.

For a new bridge deployment, use a loopback/test endpoint without the G6 attached to validate bidirectional transport before instrument testing. Do not send arbitrary loopback payloads to the G6.

## G6 acceptance

The reviewed [protocol reference](../docs/protocol/novasource-g6-command-reference.md) defines the fixed 38400/8-N-1, no-flow-control interface and documented carriage-return framing. The application validates the prompt and keeps the connection open for state queries and operator commands. It no longer provides the Sprint 0 open/close-only test.

Use the [Sprint 2 acceptance checklist](../docs/sprints/sprint-2/acceptance.md) to record actual device, firmware, port, expected results, and observations. Complete reads first, then exercise configuration/sweeps using the agreed instrument setup. Loading or storing device settings requires the application's confirmation. Record untested steps explicitly.

For recovery testing, use the simulator first. On an agreed hardware setup, interrupt the bridge during a read or sweep and confirm the application reports failure/uncertainty, stops further commands, and supports an explicit reconnect and fresh state read after the bridge recovers. Do not infer the final RF state from the last displayed value after a communication failure.

## Common failures

| Symptom | Check |
| --- | --- |
| Virtual COM port absent | Driver installation, Device Manager, com0com pair configuration |
| TCP test fails | Server process, configured address/port, routing/VPN, scoped firewall rule |
| COM access denied | Another application owns COM50, or hub4com is pointed at the wrong endpoint |
| Connect times out | Bridge ownership, server physical port, cabling, fixed serial format, RFC 2217 connection |
| Read/change fails | Restore transport, then click Connect to obtain fresh device state; do not automatically repeat writes |
| Remembered port unavailable | Restore the intended interface or select another known G6 port and click Connect |

Retain deployment package/version records. The application does not install, restart, or reconfigure hub4com.

# Bridge Deployment Settings

Copy this file to an environment-specific record outside source control if it will contain sensitive network details.

| Setting | Deployment value |
| --- | --- |
| Client computer | `CLIENT_HOSTNAME` |
| Client virtual application port | `COM50` |
| Client com0com peer | `CNCB0` |
| Bridge/server computer | `BRIDGE_HOSTNAME` |
| Bridge IP or DNS name | `BRIDGE_ADDRESS` |
| RFC 2217 TCP port | `7000` |
| Physical G6 serial port on bridge | `COM_PHYSICAL` |
| Allowed client IP | `CLIENT_IP` |
| Network protection | `Private LAN or VPN name` |
| com0com package/version | `RECORD_AFTER_DOWNLOAD` |
| com0com SHA-256 | `RECORD_AFTER_DOWNLOAD` |
| hub4com package/version | `RECORD_AFTER_DOWNLOAD` |
| hub4com SHA-256 | `RECORD_AFTER_DOWNLOAD` |
| G6 baud rate | `FROM_MANUAL` |
| Data bits | `FROM_MANUAL` |
| Parity | `FROM_MANUAL` |
| Stop bits | `FROM_MANUAL` |
| Flow control | `FROM_MANUAL` |
| Command terminator | `FROM_MANUAL` |
| Documented prompt | `FROM_MANUAL` |

Do not replace the `FROM_MANUAL` values by inference.

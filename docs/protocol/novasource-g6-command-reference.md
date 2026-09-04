# NovaSource G6 Serial Protocol Reference

## Source

Derived from *Programming the NovaSource G6*, application note NL-NS002-020315,
revision 2.0 (2008). The imported source is
[`../references/App Note Program NS G6_v2 0.pdf`](../references/App%20Note%20Program%20NS%20G6_v2%200.pdf).

SHA-256: `345C5D5FDB326F4D05149A7B43B5F3BE72654339FDAB97AF34A833CB416EEACE`

This normalized reference does not replace the model-specific user manual.

## Interface

- True-level RS-232, DB9, device wired as DTE
- Straight-through PC serial cable; no null modem
- TX pin 3, RX pin 2, ground pin 5
- 38400 baud, 8 data bits, no parity, 1 stop bit
- No handshaking; idle mark; least-significant bit first

## Protocol

- Commands are case-insensitive ASCII; responses are uppercase ASCII.
- Terminate each command with CR (`0x0D`). LF is not required.
- A space between the two-letter mnemonic and parameter is optional.
- The device does not echo individual characters.
- Backspace (`0x08`) is destructive while entering a command.
- Without CR, the device buffers input and does not respond.

The documented power-up banner is:

```text
NS G6 v1.0<CR>(c) 2002 Nova Engineering, Inc.<CR>>
```

The version is an example, not a safe fixed-version assertion. A set succeeds
with `OK<CR>>`; a query succeeds with `OK value<CR>>`. The Sprint 1 device
returned `4F-4B-20-0D-3E-20`, normalized as `OK <CR>> `, after an empty CR.

## Errors

| Response | Meaning |
| --- | --- |
| `ER 1<CR>` | Unknown command mnemonic |
| `ER 2<CR>` | Parameter outside the legal range |
| `ER 3<CR>` | Other command failure |

Timeout, maximum command length, and prompt behavior after errors are not
specified. Use bounded timeouts and retain raw transcripts.

## Commands

Omitting a parameter queries the current value unless marked read-only or
action-only.

| Command | Parameter | Function | Safety |
| --- | --- | --- | --- |
| `AT` | integer `0..31` | Attenuation step; `0` is maximum output. Approximately dB, not an absolute calibrated level. | RF mutation |
| `FR` | float, generic envelope `0.0..5875.000` MHz | Output frequency, 1 kHz resolution; actual bounds are model-specific. | RF mutation |
| `HF` | none | Highest supported frequency. | Read-only |
| `IM` | `T`, `M` | Rear input mode: trigger or modulation. | Mode mutation |
| `IT` | `E`, `D` | Internal trigger enable/disable, controlling RF output. Requires external input mode `IMM`. | RF on/off mutation |
| `LD` | none | Load nonvolatile settings and make them active. | State-changing action |
| `LF` | none | Lowest supported frequency. | Read-only |
| `LS` | none | LED, RF, and lock state as an ASCII digit. | Read-only |
| `MG` | integer `-10..+13` | Modulation gain multiplier `2^value`. | Modulation mutation |
| `MS` | `E`, `I`, `N` | External, internal 1 kHz, or no modulation. | Modulation mutation |
| `RF` | float `0.0..50.000` MHz | PLL reference frequency; documented default/internal value is 10.000 MHz. | High-impact tuning mutation |
| `RS` | `E`, `D` | RF standby enable/disable. | RF behavior mutation |
| `ST` | none | Persist current settings for power-up recall. | Persistent mutation |
| `TM` | `C`, `M`, `T` | Continuous, momentary, or toggle trigger interpretation. | Trigger mutation |

### `LS` values

| ASCII | RF | Locked | Power |
| --- | --- | --- | --- |
| `1` | off | no | on |
| `3` | off | yes | on |
| `5` | on | no | on |
| `7` | on | yes | on |

Reject other values as unexpected while retaining the raw byte.

## Implementation recommendations

1. Use COM50 at 38400/8-N-1 with handshake `None` and bounded timeouts.
2. Frame on CR and recognize `>` as ready; do not require LF.
3. Preserve raw bytes separately from normalized response lines.
4. Serialize transactions: one command plus CR, then read through prompt or timeout.
5. Emit canonical uppercase commands.
6. Validate first with empty CR, then read-only `LF`, `HF`, and `LS`.
7. Put setters behind explicit operator actions; treat `IT`, `LD`, `RF`, and `ST`
   as especially consequential.
8. Query `LF` and `HF`; never infer device frequency limits from the generic range.
9. Test success, queries, all errors, partial reads, spaces, missing prompts,
   timeouts, and alternate power-up versions using a simulator.

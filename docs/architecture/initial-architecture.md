# Initial Architecture

Historical Sprint 0 design. The application now uses persistent, bidirectional G6 communication; see the [Sprint 1 architecture record](../sprints/sprint-1/summary.md) and [current operating instructions](../development.md). The layer descriptions below describe the original prototype only.

## Boundary: remote serial connectivity

The serial-over-TCP solution is deliberately separate from the operator application. A bridge presents a remote physical port as a local virtual COM port, such as `COM50`. The application uses only the standard Windows serial-port interface.

```text
WPF application -> selected local COM port -> optional virtual COM bridge -> remote physical COM port -> NovaSource G6
```

This separation makes local and remote instruments equivalent from the application's perspective and keeps bridge deployment decisions out of the UI.

## Sprint 0 application layers

- `MainWindow` owns only WPF interaction and status presentation.
- `SerialPortProbeService` validates selection and owns the probe workflow.
- `ISerialPortProvider` and `ISerialPortConnection` isolate Windows serial APIs for unit testing.
- `WindowsSerialPortProvider` is the production adapter using `System.IO.Ports`.

No G6 command model is included until the programming manual and serial protocol are reviewed.

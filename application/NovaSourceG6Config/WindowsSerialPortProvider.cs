using System.IO.Ports;

namespace NovaSourceG6Config;

public sealed class WindowsSerialPortProvider : ISerialPortProvider
{
    public IReadOnlyList<string> GetPortNames() => SerialPort.GetPortNames();

    public ISerialPortConnection CreateConnection(string portName) => new WindowsSerialPortConnection(portName);

    private sealed class WindowsSerialPortConnection : ISerialPortConnection
    {
        private readonly SerialPort serialPort;

        public WindowsSerialPortConnection(string portName)
        {
            serialPort = new SerialPort(portName)
            {
                DtrEnable = false,
                RtsEnable = false,
                Handshake = Handshake.None,
                ReadTimeout = 250,
                WriteTimeout = 250
            };
        }

        public void Open() => serialPort.Open();

        public void Dispose() => serialPort.Dispose();
    }
}

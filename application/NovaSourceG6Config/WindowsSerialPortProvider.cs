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
                BaudRate = 38400,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DtrEnable = false,
                RtsEnable = false,
                Handshake = Handshake.None,
                ReadTimeout = 250,
                WriteTimeout = 250
            };
        }

        public void Open() => serialPort.Open();
        public void DiscardInBuffer() => serialPort.DiscardInBuffer();
        public void Write(string value) => serialPort.Write(value);
        public string ReadExisting() => serialPort.ReadExisting();
        public void Dispose() => serialPort.Dispose();
    }
}

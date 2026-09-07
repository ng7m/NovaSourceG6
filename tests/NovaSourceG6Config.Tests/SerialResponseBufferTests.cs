using NovaSourceG6Config;

namespace NovaSourceG6Config.Tests;

public class SerialResponseBufferTests
{
    [Fact]
    public void SplitReplyWaitsForPrompt()
    {
        var buffer = new SerialResponseBuffer();
        buffer.Append("O");
        Assert.False(buffer.TryRead(out _));
        buffer.Append("K\r\n");
        Assert.False(buffer.TryRead(out _));
        buffer.Append("> O");
        Assert.True(buffer.TryRead(out var frame));
        Assert.True(SerialPortProbeService.IsNovaSourcePrompt(frame));
        Assert.Equal(" O", buffer.Pending);
        buffer.Append("K 7\r\n>");
        Assert.True(buffer.TryRead(out frame));
        Assert.Equal("7", SerialPortProbeService.ParseCommandResponse("LS", frame).Value);
    }

    [Fact]
    public void CombinedRepliesAreSeparateFrames()
    {
        var buffer = new SerialResponseBuffer();
        buffer.Append("> OK\r> OK 3\r> O");
        Assert.True(buffer.TryRead(out var frame));
        Assert.Equal(">", frame);
        Assert.True(buffer.TryRead(out frame));
        Assert.True(SerialPortProbeService.ParseCommandResponse("FR 100", frame).Succeeded);
        Assert.True(buffer.TryRead(out frame));
        Assert.Equal("3", SerialPortProbeService.ParseCommandResponse("LS", frame).Value);
        Assert.False(buffer.TryRead(out _));
        Assert.Equal(" O", buffer.Pending);
        buffer.Clear();
        Assert.Equal(string.Empty, buffer.Pending);
    }

    [Theory]
    [InlineData("OK")]
    [InlineData("OKAY\r>")]
    [InlineData("K\r>")]
    public void IncompleteOrMalformedAcknowledgmentsAreRejected(string response)
    {
        Assert.False(SerialPortProbeService.ParseCommandResponse("FR 100", response).Succeeded);
    }
}

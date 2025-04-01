using System.Text;

namespace AWS.Lambda.Powertools.Common.Tests;

/// <summary>
///    Test logger output
/// </summary>
public class TestLoggerOutput : IConsoleWrapper
{
    /// <summary>
    /// Buffer for all the log messages written to the logger.
    /// </summary>
    private readonly StringBuilder _outputBuffer = new StringBuilder();
    
    /// <summary>
    /// Cleasr the output buffer.
    /// </summary>
    public void Clear()
    {
        _outputBuffer.Clear();
    }
    
    /// <summary>
    /// Output the contents of the buffer.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return _outputBuffer.ToString();
    }

    /// <inheritdoc />
    public void WriteLine(string message)
    {
        _outputBuffer.AppendLine(message);
    }

    /// <inheritdoc />
    public void Debug(string message)
    {
        _outputBuffer.AppendLine(message);
    }

    /// <inheritdoc />
    public void Error(string message)
    {
        _outputBuffer.AppendLine(message);
    }
}
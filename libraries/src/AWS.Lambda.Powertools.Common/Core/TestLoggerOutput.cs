using System;
using System.IO;
using System.Text;

namespace AWS.Lambda.Powertools.Common.Tests;

/// <summary>
///    Test logger output
/// </summary>
public class TestLoggerOutput : ISystemWrapper
{
    /// <summary>
    /// Buffer for all the log messages written to the logger.
    /// </summary>
    private readonly StringBuilder _outputBuffer = new StringBuilder();

    /// <summary>
    ///    Logs the specified value.
    /// </summary>
    /// <param name="value"></param>
    public void Log(string value)
    {
        _outputBuffer.Append(value);
    }
    
    /// <summary>
    ///    Logs the line.
    /// </summary>
    public void LogLine(string value)
    {
        _outputBuffer.AppendLine(value);
    }

    /// <summary>
    ///    Gets random number
    /// </summary>
    public double GetRandom()
    {
        return 0.7;
    }

    /// <summary>
    /// Sets console output
    ///</summary>
    public void SetOut(TextWriter writeTo)
    {
    }
    
    public void Clear()
    {
        _outputBuffer.Clear();
    }
    
    public override string ToString()
    {
        return _outputBuffer.ToString();
    }
}
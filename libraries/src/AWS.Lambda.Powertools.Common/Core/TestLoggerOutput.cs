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
    public StringBuilder Buffer { get; } = new StringBuilder();

    /// <summary>
    ///    Logs the specified value.
    /// </summary>
    /// <param name="value"></param>
    public void Log(string value)
    {
        Buffer.Append(value);
        Console.Write(value);
    }
    
    /// <summary>
    ///    Logs the line.
    /// </summary>
    public void LogLine(string value)
    {
        Buffer.AppendLine(value);
        Console.WriteLine(value);
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
        Console.SetOut(writeTo);
    }

    /// <summary>
    /// Overrides the ToString method to return the buffer as a string.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Buffer.ToString();
    }
}
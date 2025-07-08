namespace AWS.Lambda.Powertools.Common;

/// <summary>
/// Wrapper for console operations to facilitate testing by abstracting system console interactions.
/// </summary>
public interface IConsoleWrapper
{
    /// <summary>
    /// Writes the specified message followed by a line terminator to the standard output stream.
    /// </summary>
    /// <param name="message">The message to write.</param>
    void WriteLine(string message);

    /// <summary>
    /// Writes a debug message to the trace listeners in the Debug.Listeners collection.
    /// </summary>
    /// <param name="message">The debug message to write.</param>
    void Debug(string message);

    /// <summary>
    /// Writes the specified error message followed by a line terminator to the standard error stream.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    void Error(string message);
}
namespace AWS.Lambda.Powertools.Idempotency.Output;

/// <summary>
/// Does not write any log messages, all method simply return
/// </summary>
public class NullLog: ILog
{
    /// <summary>
    /// Does not write information message, simply return
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteInformation(string format, params object[] args)
    { }

    /// <summary>
    /// Does not write error message, simply return
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteError(string format, params object[] args)
    { }

    
    /// <summary>
    /// Does not write warning message, simply return
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteWarning(string format, params object[] args)
    { }

    /// <summary>
    /// Does not write debug message, simply return
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteDebug(string format, params object[] args)
    { }
}
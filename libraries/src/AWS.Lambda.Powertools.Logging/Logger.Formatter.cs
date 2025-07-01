namespace AWS.Lambda.Powertools.Logging;

public static partial class Logger
{
    /// <summary>
    ///     Set the log formatter.
    /// </summary>
    /// <param name="logFormatter">The log formatter.</param>
    /// <remarks>WARNING: This method should not be called when using AOT. ILogFormatter should be passed to PowertoolsSourceGeneratorSerializer constructor</remarks>
    public static void UseFormatter(ILogFormatter logFormatter)
    {
        Configure(config => {
            config.LogFormatter = logFormatter;
        });
    }

    /// <summary>
    ///     Set the log formatter to default.
    /// </summary>
    public static void UseDefaultFormatter()
    {
        Configure(config => {
            config.LogFormatter = null;
        });
    }
}

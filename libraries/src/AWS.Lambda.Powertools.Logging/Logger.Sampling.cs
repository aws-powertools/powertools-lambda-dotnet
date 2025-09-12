namespace AWS.Lambda.Powertools.Logging;

public static partial class Logger
{
    /// <summary>
    ///   Refresh the sampling calculation and update the minimum log level if needed
    /// </summary>
    /// <returns>True if debug sampling was enabled, false otherwise</returns>
    public static bool RefreshSampleRateCalculation()
    {
        return _config.RefreshSampleRateCalculation();
    }
}

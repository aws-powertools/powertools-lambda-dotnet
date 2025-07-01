using System;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Class LoggingAspectFactory. For "dependency inject" Aspect
/// </summary>
internal static class LoggingAspectFactory
{
    /// <summary>
    /// Get an instance of the LoggingAspect class.
    /// </summary>
    /// <param name="type">The type of the class to be logged.</param>
    /// <returns>An instance of the LoggingAspect class.</returns>
    public static object GetInstance(Type type)
    {
        return new LoggingAspect(LoggerFactoryHolder.GetOrCreateFactory().CreatePowertoolsLogger());
    }
}
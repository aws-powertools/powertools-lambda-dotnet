using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Extensions for ILoggerFactory
/// </summary>
public static class PowertoolsLoggerFactoryExtensions
{
    /// <summary>
    /// Creates a new Powertools Logger instance using the Powertools full name.
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <returns>The <see cref="ILogger"/> that was created.</returns>
    public static ILogger CreatePowertoolsLogger(this ILoggerFactory factory)
    {
        return new PowertoolsLoggerFactory(factory).CreateLogger(PowertoolsLoggerConfiguration.ConfigurationSectionName);
    }
}
using System;

namespace AWS.Lambda.Powertools.Common;

/// <inheritdoc />
public class PowertoolsEnvironment : IPowertoolsEnvironment
{
    /// <summary>
    ///     The instance
    /// </summary>
    private static IPowertoolsEnvironment _instance;
    
    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static IPowertoolsEnvironment Instance => _instance ??= new PowertoolsEnvironment();
    
    /// <inheritdoc />
    public string GetEnvironmentVariable(string variableName)
    {
        return Environment.GetEnvironmentVariable(variableName);
    }

    /// <inheritdoc />
    public void SetEnvironmentVariable(string variableName, string value)
    {
        Environment.SetEnvironmentVariable(variableName, value);
    }

    /// <inheritdoc />
    public string GetAssemblyName<T>(T type)
    {
        return type.GetType().Assembly.GetName().Name;
    }

    /// <inheritdoc />
    public string GetAssemblyVersion<T>(T type)
    {
        return type.GetType().Assembly.GetName().Version?.ToString();
    }
}
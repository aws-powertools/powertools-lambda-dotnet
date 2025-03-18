using System;
using System.Text;

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
        var version = type.GetType().Assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : string.Empty;
    }
    
    public void SetExecutionEnvironment<T>(T type)
    {
        const string envName = Constants.AwsExecutionEnvironmentVariableName;
        var envValue = new StringBuilder();
        var currentEnvValue = GetEnvironmentVariable(envName);
        var assemblyName = ParseAssemblyName(GetAssemblyName(type));

        // If there is an existing execution environment variable add the annotations package as a suffix.
        if (!string.IsNullOrEmpty(currentEnvValue))
        {
            // Avoid duplication - should not happen since the calling Instances are Singletons - defensive purposes
            if (currentEnvValue.Contains(assemblyName))
            {
                return;
            }

            envValue.Append($"{currentEnvValue} ");
        }

        var assemblyVersion = GetAssemblyVersion(type);

        envValue.Append($"{assemblyName}/{assemblyVersion}");

        SetEnvironmentVariable(envName, envValue.ToString());
    }
    
    /// <summary>
    /// Parsing the name to conform with the required naming convention for the UserAgent header (PTFeature/Name/Version)
    /// Fallback to Assembly Name on exception
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <returns></returns>
    private string ParseAssemblyName(string assemblyName)
    {
        try
        {
            var parsedName = assemblyName.Substring(assemblyName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            return $"{Constants.FeatureContextIdentifier}/{parsedName}";
        }
        catch
        {
            //NOOP
        }

        return $"{Constants.FeatureContextIdentifier}/{assemblyName}";
    }
}
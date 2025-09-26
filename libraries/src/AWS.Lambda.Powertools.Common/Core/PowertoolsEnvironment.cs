using System;
using System.Collections.Concurrent;
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
    /// Cached runtime environment string
    /// </summary>
    private static readonly string CachedRuntimeEnvironment = $"PTEnv/{Environment.GetEnvironmentVariable(Constants.AwsExecutionEnvironmentVariableName)}";
    
    /// <summary>
    /// Cache for parsed assembly names to avoid repeated string operations
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> ParsedAssemblyNameCache = new();
    
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
        if (type is Type typeObject)
        {
            return typeObject.Assembly.GetName().Name;
        }
        
        return type.GetType().Assembly.GetName().Name;
    }

    /// <inheritdoc />
    public string GetAssemblyVersion<T>(T type)
    {
        Version version;
        
        if (type is Type typeObject)
        {
            version = typeObject.Assembly.GetName().Version;
        }
        else
        {
            version = type.GetType().Assembly.GetName().Version;
        }
        
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : string.Empty;
    }
    
    /// <inheritdoc />
    public void SetExecutionEnvironment<T>(T type, string utilityName = null)
    {
        const string envName = Constants.AWSSdkUAAppId;
        var currentEnvValue = GetEnvironmentVariable(envName);
        var assemblyName = utilityName != null ? $"{Constants.FeatureContextIdentifier}/{utilityName}" : ParseAssemblyName(GetAssemblyName(type));
        var assemblyVersion = GetAssemblyVersion(type);
        var newEntry = $"{assemblyName}/{assemblyVersion}";

        // Only set if not already present and only allows one utility
        // this will change when bitwise is supported
        if (!string.IsNullOrEmpty(currentEnvValue) && (currentEnvValue.Contains(assemblyName)
                                        || currentEnvValue.Contains("PT/")))
        {
            return;
        }

        string finalValue;
        var ptenvIndex = currentEnvValue?.IndexOf("PTEnv/") ?? -1;

        if (string.IsNullOrEmpty(currentEnvValue))
        {
            finalValue = $"{newEntry} {CachedRuntimeEnvironment}";
        }
        else if (ptenvIndex >= 0)
        {
            // Insert newEntry before PTENV
            finalValue = $"{currentEnvValue.Substring(0, ptenvIndex).TrimEnd()} {newEntry} {currentEnvValue.Substring(ptenvIndex)}".Trim();
        }
        else
        {
            finalValue = $"{currentEnvValue} {newEntry} {CachedRuntimeEnvironment}";
        }

        SetEnvironmentVariable(envName, finalValue);
    }
    
    /// <summary>
    /// Parsing the name to conform with the required naming convention for the UserAgent header (PTFeature/Name/Version)
    /// Fallback to Assembly Name on exception
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <returns></returns>
    internal static string ParseAssemblyName(string assemblyName)
    {
        // Use cache to avoid repeated string operations
        try
        {
            return ParsedAssemblyNameCache.GetOrAdd(assemblyName, name =>
            {
                var lastDotIndex = name.LastIndexOf('.');
                if (lastDotIndex >= 0 && lastDotIndex < name.Length - 1)
                {
                    var parsedName = name.Substring(lastDotIndex + 1);
                    return $"{Constants.FeatureContextIdentifier}/{parsedName}";
                }

                return $"{Constants.FeatureContextIdentifier}/{name}";
            });
        }
        catch
        {
            return string.Empty;
        }
    }
}

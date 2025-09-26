namespace AWS.Lambda.Powertools.Common;

/// <summary>
/// Interface for PowertoolsEnvironment
/// </summary>
public interface IPowertoolsEnvironment
{
    /// <summary>
    /// Get environment variable by variable name
    /// </summary>
    /// <param name="variableName"></param>
    /// <returns>Environment variable</returns>
    string GetEnvironmentVariable(string variableName);
    
    /// <summary>
    /// Set environment variable
    /// </summary>
    /// <param name="variableName"></param>
    /// <param name="value">Setting this to null will remove environment variable with that name</param>
    void SetEnvironmentVariable(string variableName, string value);

    /// <summary>
    /// Get the calling Type Assembly Name
    /// </summary>
    /// <param name="type"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Assembly Name</returns>
    string GetAssemblyName<T>(T type);
    
    /// <summary>
    /// Get the calling Type Assembly Version
    /// </summary>
    /// <param name="type"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Assembly Version in the Major.Minor.Build format</returns>
    string GetAssemblyVersion<T>(T type);

    /// <summary>
    /// Sets the execution Environment Variable (AWS_SDK_UA_APP_ID)
    /// </summary>
    /// <param name="type"></param>
    /// <param name="utilityName"></param>
    void SetExecutionEnvironment<T>(T type, string utilityName = null);
}
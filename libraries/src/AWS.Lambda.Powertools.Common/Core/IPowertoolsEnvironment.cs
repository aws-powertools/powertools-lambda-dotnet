using System;

namespace AWS.Lambda.Powertools.Common;

/// <summary>
/// 
/// </summary>
public interface IPowertoolsEnvironment
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="variableName"></param>
    /// <returns></returns>
    string GetEnvironmentVariable(string variableName);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="variableName"></param>
    /// <param name="value"></param>
    void SetEnvironmentVariable(string variableName, string value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    string GetAssemblyName<T>(T type);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    string GetAssemblyVersion<T>(T type);
}

/// <inheritdoc />
public class PowertoolsEnvironment : IPowertoolsEnvironment
{
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
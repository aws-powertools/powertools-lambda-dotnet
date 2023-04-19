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
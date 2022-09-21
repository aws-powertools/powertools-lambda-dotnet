using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Provider;

public interface IParameterProviderBase
{
    Task<T?> GetAsync<T>(string key, ParameterProviderConfiguration? config, Transformation? transformation,
        string? transformerName);

    Task<IDictionary<string, string>> GetMultipleAsync(string path,
        ParameterProviderConfiguration? config, Transformation? transformation, string? transformerName);
}
using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Provider;

/// <summary>
/// Represents a type used to retrieve parameter values from a store.
/// </summary>
public interface IParameterProvider
{
    /// <summary>
    /// Get parameter value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>The parameter value.</returns>
    string? Get(string key);
    
    /// <summary>
    /// Get parameter value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>The parameter value.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Get parameter transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter transformed value.</returns>
    T? Get<T>(string key) where T : class;

    /// <summary>
    /// Get parameter transformed value for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <typeparam name="T">Target transformation type.</typeparam>
    /// <returns>The parameter transformed value.</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    IDictionary<string, string?> GetMultiple(string key);

    /// <summary>
    /// Get multiple parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/value pairs.</returns>
    Task<IDictionary<string, string?>> GetMultipleAsync(string key);
    
    /// <summary>
    /// Get multiple transformed parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/transformed value pairs.</returns>
    IDictionary<string, T?> GetMultiple<T>(string key) where T : class;

    /// <summary>
    /// Get multiple transformed parameter values for the provided key.
    /// </summary>
    /// <param name="key">The parameter key.</param>
    /// <returns>Returns a collection parameter key/transformed value pairs.</returns>
    Task<IDictionary<string, T?>> GetMultipleAsync<T>(string key) where T : class;
    
    /// <summary>
    /// Set the cache maximum age
    /// </summary>
    /// <param name="maxAge">The maximum cache age</param>
    /// <returns>Provider Configuration Builder instance</returns>
    ParameterProviderConfigurationBuilder WithMaxAge(TimeSpan maxAge);

    /// <summary>
    /// Forces provider to fetch the latest value from the store regardless if already available in cache.
    /// </summary>
    /// <returns>Provider Configuration Builder instance</returns>
    ParameterProviderConfigurationBuilder ForceFetch();

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformation">The transformation type.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    ParameterProviderConfigurationBuilder WithTransformation(Transformation transformation);

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformer">The instance of the transformer.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    ParameterProviderConfigurationBuilder WithTransformation(ITransformer transformer);

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformerName">The name of the registered transformer.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    ParameterProviderConfigurationBuilder WithTransformation(string transformerName);
}
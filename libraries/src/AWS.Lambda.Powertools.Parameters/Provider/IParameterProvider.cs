using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Provider;

/// <summary>
/// Represents a type used to retrieve parameter values from a store.
/// </summary>
public interface IParameterProvider : IParameterProvider<ParameterProviderConfigurationBuilder>
{
    
}

/// <summary>
/// Represents a type used to retrieve parameter values from a store.
/// </summary>
/// <typeparam name="TConfigurationBuilder">The type of ConfigurationBuilder</typeparam>
public interface IParameterProvider<out TConfigurationBuilder> : IParameterProviderBase
    where TConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    /// <summary>
    /// Set the cache maximum age
    /// </summary>
    /// <param name="maxAge">The maximum cache age</param>
    /// <returns>Provider Configuration Builder instance</returns>
    TConfigurationBuilder WithMaxAge(TimeSpan maxAge);

    /// <summary>
    /// Forces provider to fetch the latest value from the store regardless if already available in cache.
    /// </summary>
    /// <returns>Provider Configuration Builder instance</returns>
    TConfigurationBuilder ForceFetch();

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformation">The transformation type.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    TConfigurationBuilder WithTransformation(Transformation transformation);

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformer">The instance of the transformer.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    TConfigurationBuilder WithTransformation(ITransformer transformer);

    /// <summary>
    /// Transforms the latest value from after retrieved from the store.
    /// </summary>
    /// <param name="transformerName">The name of the registered transformer.</param>
    /// <returns>Provider Configuration Builder instance</returns>
    TConfigurationBuilder WithTransformation(string transformerName);
}
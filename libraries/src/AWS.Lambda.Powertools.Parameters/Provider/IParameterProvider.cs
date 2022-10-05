using AWS.Lambda.Powertools.Parameters.Configuration;
using AWS.Lambda.Powertools.Parameters.Transform;

namespace AWS.Lambda.Powertools.Parameters.Provider;

public interface IParameterProvider : IParameterProvider<ParameterProviderConfigurationBuilder>
{
    
}

public interface IParameterProvider<out TConfigurationBuilder> : IParameterProviderBase
    where TConfigurationBuilder : ParameterProviderConfigurationBuilder
{
    TConfigurationBuilder WithMaxAge(TimeSpan age);

    TConfigurationBuilder ForceFetch();

    TConfigurationBuilder WithTransformation(Transformation transformation);

    TConfigurationBuilder WithTransformation(ITransformer transformer);

    TConfigurationBuilder WithTransformation(string transformerName);
}
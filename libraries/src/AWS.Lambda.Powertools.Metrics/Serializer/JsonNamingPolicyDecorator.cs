using System.Text.Json;

namespace AWS.Lambda.Powertools.Metrics;

/// <summary>
///     Class JsonNamingPolicyDecorator
///     Implements the <see cref="System.Text.Json.JsonNamingPolicy" />
/// </summary>
public class JsonNamingPolicyDecorator : JsonNamingPolicy
{
    private readonly JsonNamingPolicy _underlyingNamingPolicy;

    /// <summary>
    /// JsonNamingPolicy decorator
    /// </summary>
    /// <param name="underlyingNamingPolicy">Name of the underlying JsonNamingPolicy</param>
    protected JsonNamingPolicyDecorator(JsonNamingPolicy underlyingNamingPolicy)
    {
        _underlyingNamingPolicy = underlyingNamingPolicy;
    }
    
    /// <inheritdoc />
    public override string ConvertName(string name)
    {
        return _underlyingNamingPolicy == null ? name : _underlyingNamingPolicy.ConvertName(name);
    }
}
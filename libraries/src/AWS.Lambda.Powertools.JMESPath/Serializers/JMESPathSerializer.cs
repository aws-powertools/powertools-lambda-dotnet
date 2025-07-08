using System;
using System.Text.Json;

namespace AWS.Lambda.Powertools.JMESPath.Serializers;

/// <summary>
/// Class used to serialize JMESPath types
/// </summary>
internal static class JmesPathSerializer
{
    /// <summary>
    /// Serializes the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="inputType">Type of the input.</param>
    /// <returns>System.String.</returns>
    internal static string Serialize(object value, Type inputType)
    {
#if NET6_0
        return JsonSerializer.Serialize(value);
#else

        return JsonSerializer.Serialize(value, inputType, JmesPathSerializationContext.Default);
#endif
    }
    
    /// <summary>
    /// Deserializes the specified value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">The value.</param>
    /// <returns>T.</returns>
    internal static T Deserialize<T>(string value)
    {
#if NET6_0
        return JsonSerializer.Deserialize<T>(value);
#else

        return (T)JsonSerializer.Deserialize(value, typeof(T), JmesPathSerializationContext.Default);
#endif
    }
}
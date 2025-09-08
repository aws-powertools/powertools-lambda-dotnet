

using System;

namespace AWS.Lambda.Powertools.BatchProcessing;

/// <summary>
/// Service for deserializing record data into strongly-typed objects.
/// </summary>
public interface IDeserializationService
{
    /// <summary>
    /// Deserializes the provided data string into the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="data">The data string to deserialize.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>The deserialized object of type T.</returns>
    /// <exception cref="Exceptions.DeserializationException">Thrown when deserialization fails.</exception>
    T Deserialize<T>(string data, DeserializationOptions options = null);

    /// <summary>
    /// Attempts to deserialize the provided data string into the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="data">The data string to deserialize.</param>
    /// <param name="result">When this method returns, contains the deserialized object if successful, or the default value if unsuccessful.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>true if deserialization was successful; otherwise, false.</returns>
    bool TryDeserialize<T>(string data, out T result, DeserializationOptions options = null);

    /// <summary>
    /// Attempts to deserialize the provided data string into the specified type, capturing any exception that occurs.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="data">The data string to deserialize.</param>
    /// <param name="result">When this method returns, contains the deserialized object if successful, or the default value if unsuccessful.</param>
    /// <param name="exception">When this method returns, contains the exception that occurred during deserialization if unsuccessful, or null if successful.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <returns>true if deserialization was successful; otherwise, false.</returns>
    bool TryDeserialize<T>(string data, out T result, out Exception exception, DeserializationOptions options = null);
}
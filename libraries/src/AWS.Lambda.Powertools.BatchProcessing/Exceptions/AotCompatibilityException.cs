

using System;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;

/// <summary>
/// Exception thrown when AOT (Ahead-of-Time) compilation compatibility requirements are not met.
/// </summary>
public class AotCompatibilityException : Exception
{
    /// <summary>
    /// Gets the type that caused the AOT compatibility issue.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Initializes a new instance of the AotCompatibilityException class.
    /// </summary>
    /// <param name="targetType">The type that caused the AOT compatibility issue.</param>
    /// <param name="message">The error message.</param>
    public AotCompatibilityException(Type targetType, string message)
        : base(message)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// Initializes a new instance of the AotCompatibilityException class.
    /// </summary>
    /// <param name="targetType">The type that caused the AOT compatibility issue.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AotCompatibilityException(Type targetType, string message, Exception innerException)
        : base(message, innerException)
    {
        TargetType = targetType;
    }
}
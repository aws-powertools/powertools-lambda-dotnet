

using System;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;

/// <summary>
/// Exception thrown when type validation fails in AOT scenarios.
/// </summary>
public class AotTypeValidationException : Exception
{
    /// <summary>
    /// Gets the type that failed validation.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Initializes a new instance of the AotTypeValidationException class.
    /// </summary>
    /// <param name="targetType">The type that failed validation.</param>
    /// <param name="message">The error message.</param>
    public AotTypeValidationException(Type targetType, string message)
        : base(message)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// Initializes a new instance of the AotTypeValidationException class.
    /// </summary>
    /// <param name="targetType">The type that failed validation.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AotTypeValidationException(Type targetType, string message, Exception innerException)
        : base(message, innerException)
    {
        TargetType = targetType;
    }
}
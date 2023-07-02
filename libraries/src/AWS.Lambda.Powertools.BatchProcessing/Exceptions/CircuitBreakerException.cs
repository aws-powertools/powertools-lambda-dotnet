using System;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;
internal class CircuitBreakerException : Exception
{
    internal CircuitBreakerException(string message, Exception inner) : base(message, inner)
    {
    }
}

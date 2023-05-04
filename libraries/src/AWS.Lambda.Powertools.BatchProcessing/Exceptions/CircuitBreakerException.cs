using System;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;
internal class CircuitBreakerException : Exception
{
    internal CircuitBreakerException(string message) : base(message)
    {
    }
}

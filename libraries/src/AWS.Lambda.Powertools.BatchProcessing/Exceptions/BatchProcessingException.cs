using System;
using System.Collections.Generic;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;
internal class BatchProcessingException : AggregateException
{
    internal BatchProcessingException(string message, IEnumerable<Exception> exceptions) : base(message, exceptions)
    {
    }
}

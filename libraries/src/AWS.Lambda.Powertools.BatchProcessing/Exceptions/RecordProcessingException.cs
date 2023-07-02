using System;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;
internal class RecordProcessingException : Exception
{
    internal RecordProcessingException(string message, Exception inner) : base(message, inner)
    {
    }
}

using System;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;
internal class HandleRecordException : Exception
{
    internal HandleRecordException(string message, Exception inner) : base(message, inner)
    {
    }
}

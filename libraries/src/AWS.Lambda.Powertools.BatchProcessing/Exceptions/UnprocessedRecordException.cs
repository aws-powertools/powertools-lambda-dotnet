using System;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;
internal class UnprocessedRecordException : Exception
{
    public UnprocessedRecordException(string message) : base(message)
    {
    }
}

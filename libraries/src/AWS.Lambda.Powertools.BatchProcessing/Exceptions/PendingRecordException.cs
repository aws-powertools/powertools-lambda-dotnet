using System;

namespace AWS.Lambda.Powertools.BatchProcessing.Exceptions;
internal class PendingRecordException : Exception
{
    public PendingRecordException(string message) : base(message)
    {
    }
}

using System;
using AWS.Lambda.PowerTools.Events;

namespace AWS.Lambda.PowerTools.Attributes
{
    public interface IBaseMethodAspectAttribute
    {
        void OnEntry(AspectEventArgs eventArgs);
        void OnSuccess(AspectEventArgs eventArgs, object result);
        T OnException<T>(AspectEventArgs eventArgs, Exception exception);
        void OnExit(AspectEventArgs eventArgs);
    }
}
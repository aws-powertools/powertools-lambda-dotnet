using System;

namespace AWS.Lambda.PowerTools.Aspects
{
    public interface IMethodAspectAttribute
    {
        void OnEntry(AspectEventArgs eventArgs);
        void OnSuccess(AspectEventArgs eventArgs, object result);
        T OnException<T>(AspectEventArgs eventArgs, Exception exception);
        void OnExit(AspectEventArgs eventArgs);
    }
}
using System;
using System.Threading.Tasks;
using AWS.Lambda.PowerTools.Events;

namespace AWS.Lambda.PowerTools.Attributes
{
    public abstract class BaseUniversalWrapperAttribute : Attribute
    {       
        protected internal virtual T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
        {
            return target(args);
        }
        protected internal virtual Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args, AspectEventArgs eventArgs)
        {
            return target(args);
        }
    }
}
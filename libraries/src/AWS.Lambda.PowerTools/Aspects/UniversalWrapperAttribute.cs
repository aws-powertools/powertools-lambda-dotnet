using System;
using System.Threading.Tasks;

namespace AWS.Lambda.PowerTools.Aspects
{
    public abstract class UniversalWrapperAttribute : Attribute
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
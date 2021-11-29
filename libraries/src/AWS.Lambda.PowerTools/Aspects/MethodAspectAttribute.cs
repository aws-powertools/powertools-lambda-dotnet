using System;
using System.Threading.Tasks;
using AspectInjector.Broker;

namespace AWS.Lambda.PowerTools.Aspects
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    [Injection(typeof(UniversalWrapperAspect), Inherited = true)]
    public abstract class MethodAspectAttribute : UniversalWrapperAttribute, IMethodAspectAttribute
    {
        protected internal sealed override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
        {
            OnEntry(eventArgs);
            try
            {
                var result = base.WrapSync(target, args, eventArgs);
                OnSuccess(eventArgs, result);
                return result;
            }
            catch (Exception exception)
            {
                return OnException<T>(eventArgs, exception);
            }
            finally
            {
                OnExit(eventArgs);
            }
        }

        protected internal sealed override async Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args, AspectEventArgs eventArgs)
        {
            OnEntry(eventArgs);
            try
            {
                var result = await base.WrapSync(target, args, eventArgs);
                OnSuccess(eventArgs, result);
                return result;
            }
            catch (Exception exception)
            {
                return OnException<T>(eventArgs, exception);
            }
            finally
            {
                OnExit(eventArgs);
            }
        }

        public virtual void OnEntry(AspectEventArgs eventArgs)
        {
        }

        public virtual void OnSuccess(AspectEventArgs eventArgs, object result)
        {
        }

        public virtual T OnException<T>(AspectEventArgs eventArgs, Exception exception)
        {
            throw exception;
        }
        
        public virtual void OnExit(AspectEventArgs eventArgs)
        {
        }
    }
}

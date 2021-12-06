using System;
using System.Threading.Tasks;
using AspectInjector.Broker;

namespace AWS.Lambda.PowerTools.Aspects
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    [Injection(typeof(UniversalWrapperAspect), Inherited = true)]
    public abstract class MethodAspectAttribute : UniversalWrapperAttribute
    {
        private IMethodAspectHandler _aspectHandler;
        private IMethodAspectHandler AspectHandler => _aspectHandler ??= CreateHandler();

        protected abstract IMethodAspectHandler CreateHandler();
        
        protected internal sealed override T WrapSync<T>(Func<object[], T> target, object[] args, AspectEventArgs eventArgs)
        {
            AspectHandler.OnEntry(eventArgs);
            try
            {
                var result = base.WrapSync(target, args, eventArgs);
                AspectHandler.OnSuccess(eventArgs, result);
                return result;
            }
            catch (Exception exception)
            {
                return AspectHandler.OnException<T>(eventArgs, exception);
            }
            finally
            {
                AspectHandler.OnExit(eventArgs);
            }
        }

        protected internal sealed override async Task<T> WrapAsync<T>(Func<object[], Task<T>> target, object[] args, AspectEventArgs eventArgs)
        {
            AspectHandler.OnEntry(eventArgs);
            try
            {
                var result = await base.WrapAsync(target, args, eventArgs);
                AspectHandler.OnSuccess(eventArgs, result);
                return result;
            }
            catch (Exception exception)
            {
                return AspectHandler.OnException<T>(eventArgs, exception);
            }
            finally
            {
                AspectHandler.OnExit(eventArgs);
            }
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
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
                var result = base.WrapSync(target, GetArguments(eventArgs, args), eventArgs);
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
                var result = await base.WrapSync(target, GetArguments(eventArgs, args), eventArgs);
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

        private object[] GetArguments(AspectEventArgs eventArgs, object[] args)
        {
            if (args is null || !args.Any())
                return args;

            var extraArg = GetExtraArgument(eventArgs);
            if (extraArg is null)
                return args;
            
            var parameters = ((MethodInfo) eventArgs.Method).GetParameters();
            if (parameters.Length != args.Length) 
                return args;

            var argType = extraArg.GetType();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == argType && args[i] is null)
                {
                    args[i] = extraArg;
                    break;
                }
            }
            
            return args;
        }
        
        protected virtual object GetExtraArgument(AspectEventArgs eventArgs)
        {
            return null;
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

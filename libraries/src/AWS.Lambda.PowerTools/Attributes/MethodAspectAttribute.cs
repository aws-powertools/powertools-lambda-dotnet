
using System;
using AspectInjector.Broker;
using AWS.Lambda.PowerTools.Aspects;

namespace AWS.Lambda.PowerTools.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    [Injection(typeof(MethodWrapperAspect), Inherited = true)]
    public abstract class MethodAspectAttribute : BaseMethodAspectAttribute
    {
    }
}
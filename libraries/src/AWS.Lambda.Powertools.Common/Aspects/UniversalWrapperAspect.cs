using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common.Utils;

namespace AWS.Lambda.Powertools.Common;

/// <summary>
///     Class UniversalWrapperAspect.
/// </summary>
[Aspect(Scope.Global)]
public class UniversalWrapperAspect
{
    /// <summary>
    ///     The delegate cache
    /// </summary>
    private static readonly Dictionary<MethodBase, Handler> DelegateCache = new();

    /// <summary>
    ///     Handles the specified instance.
    /// </summary>
    /// <param name="instance">The instance.</param>
    /// <param name="type">The type.</param>
    /// <param name="method">The method.</param>
    /// <param name="target">The target.</param>
    /// <param name="name">The name.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="returnType">Type of the return.</param>
    /// <param name="triggers">The triggers.</param>
    /// <returns>System.Object.</returns>
    [Advice(Kind.Around, Targets = Target.Method)]
    public object Handle(
        [Argument(Source.Instance)] object instance,
        [Argument(Source.Type)] Type type,
        [Argument(Source.Metadata)] MethodBase method,
        [Argument(Source.Target)] Func<object[], object> target,
        [Argument(Source.Name)] string name,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.ReturnType)] Type returnType,
        [Argument(Source.Triggers)] Attribute[] triggers)
    {
        var eventArgs = new AspectEventArgs
        {
            Instance = instance,
            Type = type,
            Method = method,
            Name = name,
            Args = args,
            ReturnType = returnType,
            Triggers = triggers
        };

        var wrappers = triggers.OfType<UniversalWrapperAttribute>().ToArray();
        // Target.Method is more precise for cases when decorating generic methods
        var handler = GetMethodHandler(target.Method, returnType, wrappers, method);
        return handler(target, args, eventArgs);
    }

    /// <summary>
    ///     Creates the method handler.
    /// </summary>
    /// <param name="returnType">Type of the return.</param>
    /// <param name="wrappers">The wrappers.</param>
    /// <param name="wrappedMethod">The user method being wrapped, used in error messages.</param>
    /// <returns>Handler.</returns>
    /// <exception cref="NotSupportedException">
    ///     Dynamic code is not supported (Native AOT) and the method returns a value type, <see cref="Task" />,
    ///     or <see cref="Task{TResult}" /> of a value type.
    /// </exception>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "WrapSync/WrapAsync are only instantiated over reference types when dynamic code is not supported, which use shared canonical code. Value-type instantiations are rejected before MakeGenericMethod is called.")]
    private static Handler CreateMethodHandler(Type returnType, IEnumerable<UniversalWrapperAttribute> wrappers,
        MethodBase wrappedMethod)
    {
        var targetParam = Expression.Parameter(typeof(Func<object[], object>), "orig");
        var eventArgsParam = Expression.Parameter(typeof(AspectEventArgs), "event");

        var isAsync = typeof(Task).IsAssignableFrom(returnType);
        var originalReturnType = returnType;
        Type wrapperTypeArgument;

        if (isAsync)
        {
            wrapperTypeArgument = returnType.IsConstructedGenericType
                ? returnType.GenericTypeArguments[0]
                : Type.GetType("System.Threading.Tasks.VoidTaskResult")!;
            returnType = typeof(Task<>).MakeGenericType(wrapperTypeArgument);
        }
        else
        {
            if (returnType == typeof(void))
                returnType = typeof(object);
            wrapperTypeArgument = returnType;
        }

        if (!RuntimeFeatureWrapper.IsDynamicCodeSupported && wrapperTypeArgument.IsValueType)
        {
            throw new NotSupportedException(
                $"'{wrappedMethod.DeclaringType?.Name}.{wrappedMethod.Name}' returns '{GetDisplayName(originalReturnType)}', " +
                $"which {string.Join(", ", wrappers.Select(w => w.GetType().Name).Distinct())} can't wrap when running as Native AOT. " +
                "Methods that return a value type, Task, or Task<T> of a value type are not supported under Native AOT. " +
                "Return a reference type (for example a class, record or string) or Task<T> of a reference type instead.");
        }

        // Resolve the generic method definitions here, rather than caching them in static fields,
        // so the trimmer can see which methods MakeGenericMethod is called on (avoids IL2060).
        var wrapperMethod = isAsync
            ? typeof(UniversalWrapperAttribute)
                .GetMethod(nameof(UniversalWrapperAttribute.WrapAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(wrapperTypeArgument)
            : typeof(UniversalWrapperAttribute)
                .GetMethod(nameof(UniversalWrapperAttribute.WrapSync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(wrapperTypeArgument);

        var converArgs = Expression.Parameter(typeof(object[]), "args");
        var next = Expression.Lambda(Expression.Convert(Expression.Invoke(targetParam, converArgs), returnType),
            converArgs);

        foreach (var wrapper in wrappers)
        {
            var argsParam = Expression.Parameter(typeof(object[]), "args");
            next = Expression.Lambda(
                Expression.Call(Expression.Constant(wrapper), wrapperMethod, next, argsParam, eventArgsParam),
                argsParam);
        }

        var origArgs = Expression.Parameter(typeof(object[]), "orig_args");
        var handler = Expression.Lambda<Handler>(Expression.Convert(Expression.Invoke(next, origArgs), typeof(object)),
            targetParam, origArgs, eventArgsParam);

        var handlerCompiled = handler.Compile();

        return handlerCompiled;
    }

    /// <summary>
    ///     Gets the method handler.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="returnType">Type of the return.</param>
    /// <param name="wrappers">The wrappers.</param>
    /// <param name="wrappedMethod">The user method being wrapped, used in error messages.</param>
    /// <returns>Handler.</returns>
    private static Handler GetMethodHandler(MethodBase method, Type returnType,
        IEnumerable<UniversalWrapperAttribute> wrappers, MethodBase wrappedMethod)
    {
        lock (method)
        {
            if (!DelegateCache.TryGetValue(method, out var handler))
                if (!DelegateCache.TryGetValue(method, out handler))
                    DelegateCache[method] = handler = CreateMethodHandler(returnType, wrappers, wrappedMethod);

            return handler;
        }
    }

    private static string GetDisplayName(Type type)
    {
        if (!type.IsConstructedGenericType)
            return type.Name;

        var name = type.Name.Substring(0, type.Name.IndexOf('`'));
        return $"{name}<{string.Join(", ", type.GenericTypeArguments.Select(GetDisplayName))}>";
    }

    /// <summary>
    ///     Delegate Handler
    /// </summary>
    /// <param name="next">The next.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="eventArgs">The <see cref="AspectEventArgs" /> instance containing the event data.</param>
    /// <returns>System.Object.</returns>
    private delegate object Handler(Func<object[], object> next, object[] args, AspectEventArgs eventArgs);
}
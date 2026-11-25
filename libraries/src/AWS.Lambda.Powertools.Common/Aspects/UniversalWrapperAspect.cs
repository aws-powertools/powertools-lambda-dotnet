using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    private static Handler CreateMethodHandler(Type returnType, UniversalWrapperAttribute[] wrappers,
        MethodBase wrappedMethod)
    {
        if (returnType == typeof(void))
            return CreateSyncHandler<object>(wrappers);

        var isAsync = typeof(Task).IsAssignableFrom(returnType);
        if (isAsync && !returnType.IsConstructedGenericType)
            return CreateTaskHandler(wrappers);

        var resultType = isAsync ? returnType.GenericTypeArguments[0] : returnType;
        return CreateResultHandler(resultType, isAsync, wrappers, returnType, wrappedMethod);
    }

    /// <summary>
    ///     Creates the handler for a method that returns <paramref name="resultType" />, or
    ///     <see cref="Task{TResult}" /> of it when <paramref name="isAsync" /> is true.
    /// </summary>
    /// <remarks>
    ///     Under Native AOT, generic code over a value type only exists if the compiler sees that instantiation.
    ///     Naming the instantiations for common value types here makes the compiler generate
    ///     <see cref="UniversalWrapperAttribute.WrapSync{T}" /> and <see cref="UniversalWrapperAttribute.WrapAsync{T}" />
    ///     (and their overrides) for them. Other types go through <see cref="CreateHandlerWithReflection" />.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///     Dynamic code is not supported (Native AOT) and <paramref name="resultType" /> is a value type that isn't
    ///     covered here.
    /// </exception>
    private static Handler CreateResultHandler(Type resultType, bool isAsync, UniversalWrapperAttribute[] wrappers,
        Type returnType, MethodBase wrappedMethod)
    {
        if (resultType == typeof(int))
            return CreateHandler<int>(isAsync, wrappers);
        if (resultType == typeof(long))
            return CreateHandler<long>(isAsync, wrappers);
        if (resultType == typeof(bool))
            return CreateHandler<bool>(isAsync, wrappers);
        if (resultType == typeof(double))
            return CreateHandler<double>(isAsync, wrappers);
        if (resultType == typeof(decimal))
            return CreateHandler<decimal>(isAsync, wrappers);
        if (resultType == typeof(Guid))
            return CreateHandler<Guid>(isAsync, wrappers);
        if (resultType == typeof(DateTime))
            return CreateHandler<DateTime>(isAsync, wrappers);

        if (!RuntimeFeatureWrapper.IsDynamicCodeSupported && resultType.IsValueType)
        {
            throw new NotSupportedException(
                $"'{wrappedMethod.DeclaringType?.Name}.{wrappedMethod.Name}' returns '{GetDisplayName(returnType)}', " +
                $"which {string.Join(", ", wrappers.Select(w => w.GetType().Name).Distinct())} can't wrap when running as Native AOT. " +
                "Under Native AOT the supported return types are void, Task, reference types (for example a class, record or string), " +
                "and int, long, bool, double, decimal, Guid or DateTime, either directly or as Task<T>. " +
                "Return one of these types instead, for example by wrapping the value in a class or record.");
        }

        return CreateHandlerWithReflection(resultType, isAsync, wrappers);
    }

    private static Handler CreateHandler<T>(bool isAsync, UniversalWrapperAttribute[] wrappers)
    {
        return isAsync ? CreateAsyncHandler<T>(wrappers) : CreateSyncHandler<T>(wrappers);
    }

    /// <summary>
    ///     Creates the handler for any other result type, by instantiating <see cref="CreateSyncHandler{T}" /> or
    ///     <see cref="CreateAsyncHandler{T}" /> over <paramref name="resultType" />.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "When dynamic code is not supported, only reference types reach MakeGenericMethod, and those use shared canonical code. Value types are either covered by CreateResultHandler or rejected before this call.")]
    [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
        Justification = "CreateSyncHandler/CreateAsyncHandler are private members of this class. They are resolved here, not in static fields, so the trimmer can see which method MakeGenericMethod is called on.")]
    private static Handler CreateHandlerWithReflection(Type resultType, bool isAsync,
        UniversalWrapperAttribute[] wrappers)
    {
        var factory = isAsync
            ? typeof(UniversalWrapperAspect).GetMethod(nameof(CreateAsyncHandler), BindingFlags.NonPublic | BindingFlags.Static)
            : typeof(UniversalWrapperAspect).GetMethod(nameof(CreateSyncHandler), BindingFlags.NonPublic | BindingFlags.Static);

        return (Handler)(factory ?? throw new InvalidOperationException("Could not resolve the handler factory."))
            .MakeGenericMethod(resultType)
            .Invoke(null, new object[] { wrappers })!;
    }

    /// <summary>
    ///     Creates a handler for a method that returns <typeparamref name="T" />. Each wrapper wraps the previous one,
    ///     so the last wrapper runs first.
    /// </summary>
    private static Handler CreateSyncHandler<T>(UniversalWrapperAttribute[] wrappers)
    {
        return (target, args, eventArgs) =>
        {
            Func<object[], T> next = a => (T)target(a);
            foreach (var wrapper in wrappers)
            {
                var inner = next;
                next = a => wrapper.WrapSync(inner, a, eventArgs);
            }

            // Null for void methods and methods that return null, as before.
            return next(args)!;
        };
    }

    /// <summary>
    ///     Creates a handler for a method that returns <see cref="Task{TResult}" /> of <typeparamref name="T" />.
    /// </summary>
    private static Handler CreateAsyncHandler<T>(UniversalWrapperAttribute[] wrappers)
    {
        return (target, args, eventArgs) => ChainAsync(wrappers, a => (Task<T>)target(a), eventArgs)(args);
    }

    /// <summary>
    ///     Creates a handler for a method that returns a plain <see cref="Task" />. The wrappers see it as
    ///     <c>Task&lt;object&gt;</c>, so no value-type instantiation (<c>Task&lt;VoidTaskResult&gt;</c>) is needed.
    /// </summary>
    private static Handler CreateTaskHandler(UniversalWrapperAttribute[] wrappers)
    {
        return (target, args, eventArgs) => ChainAsync(wrappers, a => AsObjectTask((Task)target(a)), eventArgs)(args);
    }

    /// <summary>
    ///     Wraps <paramref name="next" /> in each wrapper's <see cref="UniversalWrapperAttribute.WrapAsync{T}" />,
    ///     so the last wrapper runs first.
    /// </summary>
    private static Func<object[], Task<T>> ChainAsync<T>(UniversalWrapperAttribute[] wrappers,
        Func<object[], Task<T>> next, AspectEventArgs eventArgs)
    {
        foreach (var wrapper in wrappers)
        {
            var inner = next;
            next = a => wrapper.WrapAsync(inner, a, eventArgs);
        }

        return next;
    }

    /// <summary>
    ///     The result of a wrapped plain <see cref="Task" />. It serializes as <c>{}</c>, as <c>VoidTaskResult</c> did,
    ///     so results stored by earlier versions (for example idempotency records) stay compatible.
    /// </summary>
    private static readonly object TaskResult = new();

    private static async Task<object> AsObjectTask(Task task)
    {
        await task.ConfigureAwait(false);
        return TaskResult;
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
        UniversalWrapperAttribute[] wrappers, MethodBase wrappedMethod)
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

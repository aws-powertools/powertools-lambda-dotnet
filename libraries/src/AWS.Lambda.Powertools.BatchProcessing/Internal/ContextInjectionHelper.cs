

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.BatchProcessing.Internal;

/// <summary>
/// Helper class for automatic Lambda context injection in typed record handlers.
/// </summary>
internal static class ContextInjectionHelper
{
    /// <summary>
    /// Determines if a delegate requires Lambda context by examining its method signature.
    /// </summary>
    /// <param name="handler">The handler delegate to examine.</param>
    /// <returns>True if the handler requires Lambda context, false otherwise.</returns>
    public static bool RequiresContext(Delegate handler)
    {
        if (handler == null)
            return false;

        var method = handler.Method;
        var parameters = method.GetParameters();

        // Check if any parameter is of type ILambdaContext
        return parameters.Any(p => p.ParameterType == typeof(ILambdaContext));
    }

    /// <summary>
    /// Invokes a delegate with automatic context injection based on method signature.
    /// </summary>
    /// <typeparam name="T">The type of the deserialized data.</typeparam>
    /// <param name="handler">The handler delegate to invoke.</param>
    /// <param name="data">The deserialized data to pass to the handler.</param>
    /// <param name="context">The Lambda context (can be null).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the handler invocation.</returns>
    public static async Task<RecordHandlerResult> InvokeWithContextInjection<T>(
        Delegate handler,
        T data,
        ILambdaContext context,
        CancellationToken cancellationToken)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var method = handler.Method;
        var parameters = method.GetParameters();
        var args = new object[parameters.Length];

        // Build arguments array based on parameter types
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            
            if (paramType == typeof(T))
            {
                args[i] = data;
            }
            else if (paramType == typeof(ILambdaContext))
            {
                args[i] = context; // Can be null
            }
            else if (paramType == typeof(CancellationToken))
            {
                args[i] = cancellationToken;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported parameter type '{paramType.Name}' in handler method signature. " +
                    $"Supported types are: {typeof(T).Name}, {nameof(ILambdaContext)}, {nameof(CancellationToken)}.");
            }
        }

        // Invoke the handler
        var result = handler.DynamicInvoke(args);
        
        // Handle both sync and async results
        if (result is Task<RecordHandlerResult> asyncResult)
        {
            return await asyncResult;
        }

        if (result is RecordHandlerResult syncResult)
        {
            return syncResult;
        }

        if (result is Task task)
        {
            await task;
            return RecordHandlerResult.None;
        }

        throw new InvalidOperationException(
            $"Handler method must return either {nameof(RecordHandlerResult)} or Task<{nameof(RecordHandlerResult)}>.");
    }

    /// <summary>
    /// Creates a wrapper that adapts any delegate to ITypedRecordHandlerWithContext with automatic context injection.
    /// </summary>
    /// <typeparam name="T">The type of the deserialized data.</typeparam>
    /// <param name="handler">The handler delegate to wrap.</param>
    /// <returns>A wrapper that implements ITypedRecordHandlerWithContext.</returns>
    public static ITypedRecordHandlerWithContext<T> CreateContextAwareWrapper<T>(Delegate handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        return new DelegateContextAwareWrapper<T>(handler);
    }

    /// <summary>
    /// Validates that a handler delegate has a supported method signature.
    /// </summary>
    /// <typeparam name="T">The expected data type.</typeparam>
    /// <param name="handler">The handler delegate to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the handler has an invalid signature.</exception>
    public static void ValidateHandlerSignature<T>(Delegate handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var method = handler.Method;
        var parameters = method.GetParameters();
        
        // Must have at least one parameter (the data)
        if (parameters.Length == 0)
        {
            throw new ArgumentException("Handler method must have at least one parameter for the deserialized data.");
        }

        // First parameter must be of type T
        if (parameters[0].ParameterType != typeof(T))
        {
            throw new ArgumentException(
                $"First parameter of handler method must be of type '{typeof(T).Name}', but was '{parameters[0].ParameterType.Name}'.");
        }

        // Validate all parameter types
        foreach (var param in parameters)
        {
            var paramType = param.ParameterType;
            if (paramType != typeof(T) && 
                paramType != typeof(ILambdaContext) && 
                paramType != typeof(CancellationToken))
            {
                throw new ArgumentException(
                    $"Unsupported parameter type '{paramType.Name}' in handler method signature. " +
                    $"Supported types are: {typeof(T).Name}, {nameof(ILambdaContext)}, {nameof(CancellationToken)}.");
            }
        }

        // Validate return type
        var returnType = method.ReturnType;
        if (returnType != typeof(RecordHandlerResult) && 
            returnType != typeof(Task<RecordHandlerResult>) &&
            returnType != typeof(Task) &&
            returnType != typeof(void))
        {
            throw new ArgumentException(
                $"Handler method must return {nameof(RecordHandlerResult)}, Task<{nameof(RecordHandlerResult)}>, Task, or void. " +
                $"Actual return type: {returnType.Name}");
        }
    }

    /// <summary>
    /// Internal wrapper class that implements ITypedRecordHandlerWithContext for any delegate.
    /// </summary>
    private sealed class DelegateContextAwareWrapper<T> : ITypedRecordHandlerWithContext<T>
    {
        private readonly Delegate _handler;

        public DelegateContextAwareWrapper(Delegate handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            ValidateHandlerSignature<T>(_handler);
        }

        public async Task<RecordHandlerResult> HandleAsync(T data, ILambdaContext context, CancellationToken cancellationToken)
        {
            return await InvokeWithContextInjection(_handler, data, context, cancellationToken);
        }
    }
}
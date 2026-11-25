using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Core;
using AWS.Lambda.Powertools.Common.Utils;

namespace AWS.Lambda.Powertools.Tracing.Internal;

/// <summary>
///     Tracing Aspect
///     Scope.Global is singleton
/// </summary>
[Aspect(Scope.Global, Factory = typeof(TracingAspectFactory))]
public class TracingAspect
{
    /// <summary>
    ///     The Powertools for AWS Lambda (.NET) configurations
    /// </summary>
    private readonly IPowertoolsConfigurations _powertoolsConfigurations;

    /// <summary>
    ///     X-Ray Recorder
    /// </summary>
    private readonly IXRayRecorder _xRayRecorder;

    /// <summary>
    ///     Thread-safe flag for capturing annotations. Uses int for Interlocked operations.
    ///     1 = should capture, 0 = already captured
    /// </summary>
    private static int _captureAnnotations = 1;

    /// <summary>
    ///     If true, annotations have been captured by this invocation's execution context
    /// </summary>
    private static readonly AsyncLocal<bool> _isAnnotationsCaptured = new();

    /// <summary>
    /// Aspect constructor
    /// </summary>
    /// <param name="powertoolsConfigurations"></param>
    /// <param name="xRayRecorder"></param>
    public TracingAspect(IPowertoolsConfigurations powertoolsConfigurations, IXRayRecorder xRayRecorder)
    {
        _powertoolsConfigurations = powertoolsConfigurations;
        _xRayRecorder = xRayRecorder;
    }

    /// <summary>
    ///     Cached readers of <see cref="Task{TResult}.Result" />, one per result type.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Func<Task, object>> TaskResultReaders = new();

    /// <summary>
    ///     Reads the result of a <see cref="Task{TResult}" /> without reflecting over its <c>Result</c> property,
    ///     whose metadata Native AOT removes.
    /// </summary>
    /// <returns>
    ///     False when the task isn't a <see cref="Task{TResult}" />, or when <c>TResult</c> is a value type and dynamic
    ///     code isn't supported (Native AOT): the generic reader has no native code for value types there.
    /// </returns>
    private static bool TryGetTaskResult(Task task, out object result)
    {
        result = null;

        var resultType = GetTaskResultType(task.GetType());
        if (resultType is null)
            return false;

        if (resultType.IsValueType && !RuntimeFeatureWrapper.IsDynamicCodeSupported)
            return false;

        result = TaskResultReaders.GetOrAdd(resultType, CreateTaskResultReader)(task);
        return true;
    }

    private static Type GetTaskResultType(Type taskType)
    {
        // Async methods return a subclass of Task<TResult> (the async state machine box), so walk the base types.
        // taskType derives from Task, so the loop always ends there.
        for (var type = taskType; type != typeof(Task); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
                return type.GetGenericArguments()[0];
        }

        return null;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "ReadTaskResult is only instantiated over reference types when dynamic code is not supported " +
                        "(see TryGetTaskResult), which use shared canonical code.")]
    private static Func<Task, object> CreateTaskResultReader(Type resultType)
    {
        // Resolved here, not in a static field, so the trimmer can see which method MakeGenericMethod is called on
        return typeof(TracingAspect).GetMethod(nameof(ReadTaskResult), BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(resultType)
            .CreateDelegate<Func<Task, object>>();
    }

    private static object ReadTaskResult<TResult>(Task task)
    {
        return ((Task<TResult>)task).Result;
    }

    /// <summary>
    /// Surrounds the specific method with Tracing aspect
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="target"></param>
    /// <param name="triggers"></param>
    /// <returns></returns>
    [Advice(Kind.Around)]
    public object Around(
        [Argument(Source.Name)] string name,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Target)] Func<object[], object> target,
        [Argument(Source.Triggers)] Attribute[] triggers)
    {
        var trigger = triggers.OfType<TracingAttribute>().First();

        if (TracingDisabled())
            return target(args);

        var @namespace = !string.IsNullOrWhiteSpace(trigger.Namespace)
            ? trigger.Namespace
            : _powertoolsConfigurations.Service;

        var (segmentName, metadataName) = string.IsNullOrWhiteSpace(trigger.SegmentName)
            ? ($"## {name}", name)
            : (trigger.SegmentName, trigger.SegmentName);

        BeginSegment(segmentName, @namespace);

        try
        {
            var result = target(args);

            if (result is Task task)
            {
                if (task.IsFaulted)  
                {  
                    // Force the exception to be thrown  
                    task.Exception?.Handle(ex => false);    
                } 
                
                // Only handle response if the task has a result (Task<TResult>)
                if (TryGetTaskResult(task, out var taskResult))
                {
                    HandleResponse(metadataName, taskResult, trigger.CaptureMode, @namespace);
                }
                _xRayRecorder.EndSubsegment();
                return task;
            }

            HandleResponse(metadataName, result, trigger.CaptureMode, @namespace);

            _xRayRecorder.EndSubsegment();
            return result;
        }
        catch (Exception ex)
        {
            var actualException = ex is AggregateException ae ? ae.InnerException! : ex;  
            HandleException(actualException, metadataName, trigger.CaptureMode, @namespace);  
            _xRayRecorder.EndSubsegment();  
            
            // Capture and rethrow the original exception preserving the stack trace
            ExceptionDispatchInfo.Capture(actualException).Throw();  
            throw;
        }
        finally
        {
            // Reset the capture flag if this execution context captured annotations
            if (_isAnnotationsCaptured.Value)
            {
                Interlocked.Exchange(ref _captureAnnotations, 1);
                _isAnnotationsCaptured.Value = false;
            }
        }
    }

    private void BeginSegment(string segmentName, string @namespace)
    {
        _xRayRecorder.BeginSubsegment(segmentName);
        _xRayRecorder.SetNamespace(@namespace);

        // Use Interlocked.CompareExchange for thread-safe check-and-set
        // Only one thread will successfully change from 1 to 0
        if (Interlocked.CompareExchange(ref _captureAnnotations, 0, 1) == 1)
        {
            _xRayRecorder.AddAnnotation("ColdStart", LambdaLifecycleTracker.IsColdStart);
            _isAnnotationsCaptured.Value = true;

            if (_powertoolsConfigurations.IsServiceDefined)
                _xRayRecorder.AddAnnotation("Service", _powertoolsConfigurations.Service);
        }
    }

    private void HandleResponse(string name, object result, TracingCaptureMode captureMode, string @namespace)
    {
        if (!CaptureResponse(captureMode)) return;
        if (result == null) return;  // Don't try to serialize null results

        // Skip if the result is VoidTaskResult
        if (result.GetType().Name == "VoidTaskResult") return;

        if (!RuntimeFeatureWrapper.IsDynamicCodeSupported) // is AOT
        {
            _xRayRecorder.AddMetadata(
                @namespace,
                $"{name} response",
                Serializers.PowertoolsTracingSerializer.Serialize(result)
            );
            return;
        }

        _xRayRecorder.AddMetadata(
            @namespace,
            $"{name} response",
            result
        );
    }

    private void HandleException(Exception exception, string name, TracingCaptureMode captureMode, string @namespace)
    {
        if (!CaptureError(captureMode)) return;

        var sb = new StringBuilder();
        sb.AppendLine($"Exception type: {exception.GetType()}");
        sb.AppendLine($"Exception message: {exception.Message}");
        sb.AppendLine($"Stack trace: {exception.StackTrace}");

        if (exception.InnerException != null)
        {
            sb.AppendLine("---BEGIN InnerException--- ");
            sb.AppendLine($"Exception type {exception.InnerException.GetType()}");
            sb.AppendLine($"Exception message: {exception.InnerException.Message}");
            sb.AppendLine($"Stack trace: {exception.InnerException.StackTrace}");
            sb.AppendLine("---END Inner Exception");
        }

        _xRayRecorder.AddMetadata(
            @namespace,
            $"{name} error",
            sb.ToString()
        );
    }

    private bool TracingDisabled()
    {
        if (_powertoolsConfigurations.TracingDisabled)
        {
            Console.WriteLine("Tracing has been disabled via env var POWERTOOLS_TRACE_DISABLED");
            return true;
        }

        if (!_powertoolsConfigurations.IsLambdaEnvironment)
        {
            Console.WriteLine("Running outside Lambda environment; disabling Tracing");
            return true;
        }

        return false;
    }

    private bool CaptureResponse(TracingCaptureMode captureMode)
    {
        return captureMode switch
        {
            TracingCaptureMode.EnvironmentVariable => _powertoolsConfigurations.TracerCaptureResponse,
            TracingCaptureMode.Response => true,
            TracingCaptureMode.ResponseAndError => true,
            _ => false
        };
    }

    private bool CaptureError(TracingCaptureMode captureMode)
    {
        return captureMode switch
        {
            TracingCaptureMode.EnvironmentVariable => _powertoolsConfigurations.TracerCaptureError,
            TracingCaptureMode.Error => true,
            TracingCaptureMode.ResponseAndError => true,
            _ => false
        };
    }

    internal static void ResetForTest()
    {
        LambdaLifecycleTracker.Reset();
        Interlocked.Exchange(ref _captureAnnotations, 1);
        _isAnnotationsCaptured.Value = false;
    }
}
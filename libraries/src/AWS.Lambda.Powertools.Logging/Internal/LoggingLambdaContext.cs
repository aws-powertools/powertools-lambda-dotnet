using System;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Lambda Context
/// </summary>
public class LoggingLambdaContext
{
    /// <summary>
    /// The AWS request ID associated with the request.
    /// This is the same ID returned to the client that called invoke().
    /// This ID is reused for retries on the same request.
    /// </summary>
    internal string AwsRequestId { get; private set; }

    /// <summary>Name of the Lambda function that is running.</summary>
    internal string FunctionName { get; private set; }

    /// <summary>
    /// The Lambda function version that is executing.
    /// If an alias is used to invoke the function, then this will be
    /// the version the alias points to.
    /// </summary>
    internal string FunctionVersion { get; private set; }

    /// <summary>
    /// The ARN used to invoke this function.
    /// It can be function ARN or alias ARN.
    /// An unqualified ARN executes the $LATEST version and aliases execute
    /// the function version they are pointing to.
    /// </summary>
    internal string InvokedFunctionArn { get; private set; }

    /// <summary>
    /// The CloudWatch log group name associated with the invoked function.
    /// It can be null if the IAM user provided does not have permission for
    /// CloudWatch actions.
    /// </summary>
    internal string LogGroupName { get; private set; }

    /// <summary>
    /// The CloudWatch log stream name for this function execution.
    /// It can be null if the IAM user provided does not have permission
    /// for CloudWatch actions.
    /// </summary>
    internal string LogStreamName { get; private set; }

    /// <summary>
    /// Memory limit, in MB, you configured for the Lambda function.
    /// </summary>
    internal int MemoryLimitInMB { get; private set; }

    /// <summary>
    ///     The instance
    /// </summary>
    internal static LoggingLambdaContext Instance { get; private set; }

    /// <summary>
    /// Gets the Lambda context
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static bool Extract(AspectEventArgs args)
    {
        if (Instance is not null)
            return false;

        if (args?.Args is null)
            return false;
        if (args.Method is null)
            return false;
        
        var index = Array.FindIndex(args.Method.GetParameters(), p => p.ParameterType == typeof(ILambdaContext));
        if (index >= 0)
        {
            var x = (ILambdaContext)args.Args[index];

            Instance = new LoggingLambdaContext
            {
                AwsRequestId = x.AwsRequestId,
                FunctionName = x.FunctionName,
                FunctionVersion = x.FunctionVersion,
                InvokedFunctionArn = x.InvokedFunctionArn,
                LogGroupName = x.LogGroupName,
                LogStreamName = x.LogStreamName,
                MemoryLimitInMB = x.MemoryLimitInMB
            };
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Clear the extracted lambda context.
    /// </summary>
    internal static void Clear()
    {
        Instance = null;
    }
}
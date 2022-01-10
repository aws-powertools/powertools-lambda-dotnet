using System.Linq;

namespace AWS.Lambda.Powertools.Common;

internal class PowertoolsLambdaContext
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
    internal static PowertoolsLambdaContext Instance { get; private set; }
    
    /// <summary>
    ///     Initializes a new instance of the <see cref="PowertoolsLambdaContext" /> class.
    /// </summary>
    /// <param name="awsRequestId">The AWS request ID associated with the request.</param>
    /// <param name="functionName">Name of the Lambda function that is running.</param>
    /// <param name="functionVersion">The Lambda function version that is executing.</param>
    /// <param name="invokedFunctionArn">The ARN used to invoke this function.</param>
    /// <param name="logGroupName">The CloudWatch log group name associated with the invoked function.</param>
    /// <param name="logStreamName">The CloudWatch log stream name for this function execution.</param>
    /// <param name="memoryLimitInMB">Memory limit, in MB, you configured for the Lambda function.</param>
    protected PowertoolsLambdaContext
    (
        string awsRequestId,
        string functionName,
        string functionVersion,
        string invokedFunctionArn,
        string logGroupName,
        string logStreamName,
        int memoryLimitInMB
    )
    {
        AwsRequestId = awsRequestId;
        FunctionName = functionName;
        FunctionVersion = functionVersion;
        InvokedFunctionArn = invokedFunctionArn;
        LogGroupName = logGroupName;
        LogStreamName = logStreamName;
        MemoryLimitInMB = memoryLimitInMB;
    }

    /// <summary>
    ///     Extract the lambda context from Lambda handler arguments.
    /// </summary>
    /// <param name="eventArgs">
    ///     The <see cref="T:AWS.Lambda.Powertools.Aspects.AspectEventArgs" /> instance containing the
    ///     event data.
    /// </param>
    internal static bool Extract(AspectEventArgs eventArgs)
    {
        if (Instance is not null)
            return false;

        dynamic lambdaContext = eventArgs?.Args?.LastOrDefault(arg => (arg.GetType().Name ?? "").EndsWith("LambdaContext"));;

        if (lambdaContext is null)
            return false;

        Instance = new PowertoolsLambdaContext
        (
            awsRequestId: lambdaContext.AwsRequestId,
            functionName: lambdaContext.FunctionName,
            functionVersion: lambdaContext.FunctionVersion,
            invokedFunctionArn: lambdaContext.InvokedFunctionArn,
            logGroupName: lambdaContext.LogGroupName,
            logStreamName: lambdaContext.LogStreamName,
            memoryLimitInMB: lambdaContext.MemoryLimitInMB
        );
        return true;
    }
    
    /// <summary>
    ///     Clear the extracted lambda context.
    /// </summary>
    internal static void Clear()
    {
        Instance = null;
    }
}
using System;

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

        if (eventArgs?.Args is null)
            return false;

        foreach (var arg in eventArgs.Args)
        {
            if (arg is null)
                continue;

            var argType = arg.GetType();
            if (!argType.Name.EndsWith("LambdaContext"))
                continue;

            Instance = new PowertoolsLambdaContext();
            
            foreach (var prop in argType.GetProperties())
            {
                if (prop.Name.Equals("AwsRequestId", StringComparison.CurrentCultureIgnoreCase))
                {
                    Instance.AwsRequestId = prop.GetValue(arg) as string;
                }
                else if (prop.Name.Equals("FunctionName", StringComparison.CurrentCultureIgnoreCase))
                {
                    Instance.FunctionName = prop.GetValue(arg) as string;
                }
                else if (prop.Name.Equals("FunctionVersion", StringComparison.CurrentCultureIgnoreCase))
                {
                    Instance.FunctionVersion = prop.GetValue(arg) as string;
                }
                else if (prop.Name.Equals("InvokedFunctionArn", StringComparison.CurrentCultureIgnoreCase))
                {
                    Instance.InvokedFunctionArn = prop.GetValue(arg) as string;
                }
                else if (prop.Name.Equals("LogGroupName", StringComparison.CurrentCultureIgnoreCase))
                {
                    Instance.LogGroupName = prop.GetValue(arg) as string;
                }
                else if (prop.Name.Equals("LogStreamName", StringComparison.CurrentCultureIgnoreCase))
                {
                    Instance.LogStreamName = prop.GetValue(arg) as string;
                }
                else if (prop.Name.Equals("MemoryLimitInMB", StringComparison.CurrentCultureIgnoreCase))
                {
                    var propVal = prop.GetValue(arg);
                    if (propVal is null || !int.TryParse(propVal.ToString(), out var intVal)) continue;
                    Instance.MemoryLimitInMB = intVal;
                }
            }

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
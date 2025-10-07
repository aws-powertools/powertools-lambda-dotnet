using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace AWS.Lambda.Powertools.Tracing.Tests.Handlers;

public class ExceptionFunctionHandler
{
    [Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
    public async Task<string> Handle(string input)
    {
        ThisThrows();

        await Task.Delay(1);

        return input.ToUpper(CultureInfo.InvariantCulture);
    }

    private void ThisThrows()
    {
        throw new NullReferenceException();
    }
}

public class HandlerWithNotSupportedTypes
{
    [Tracing]
    public object Handle(string input, ILambdaContext context)
    {
        return new
        {
            SystemInfo = new
            {
                DotNetVersion = Environment.Version.ToString(),
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                OSDescription = RuntimeInformation.OSDescription,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                Is64BitProcess = Environment.Is64BitProcess,
                CLRVersion = Environment.Version.ToString(),
                CurrentDirectory = Environment.CurrentDirectory
            },
            LambdaInfo = new
            {
                FunctionName = context.FunctionName,
                FunctionVersion = context.FunctionVersion,
                InvokedFunctionArn = context.InvokedFunctionArn,
                MemoryLimitInMB = context.MemoryLimitInMB,
                RemainingTime = context.RemainingTime,
                RequestId = context.AwsRequestId,
                LogGroupName = context.LogGroupName,
                LogStreamName = context.LogStreamName
            }
        };
    }
}
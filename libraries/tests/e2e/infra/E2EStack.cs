using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace E2E
{
    public class E2EStack : Stack
    {
        internal E2EStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            new FunctionConstruct(this, "lambda_x86_net8", new FunctionConstructProps
            {
                Runtime = Runtime.DOTNET_8,
                Architecture = Architecture.X86_64,
                Name = "E2ETestLambda_X86_NET8",
                Handler = "LoggingFunction::LoggingFunction.Function::FunctionHandler",
                SourcePath = "../lambdas/LoggingFunction/src/LoggingFunction",
                DistPath = "../lambdas/LoggingFunction/dist"
            });
            new FunctionConstruct(this, "lambda_arm_net8", new FunctionConstructProps
            {
                Runtime = Runtime.DOTNET_8,
                Architecture = Architecture.ARM_64,
                Name = "E2ETestLambda_ARM_NET8",
                Handler = "LoggingFunction::LoggingFunction.Function::FunctionHandler",
                SourcePath = "../lambdas/LoggingFunction/src/LoggingFunction",
                DistPath = "../lambdas/LoggingFunction/dist"
            });
            new FunctionConstruct(this, "lambda_x86_net6", new FunctionConstructProps
            {
                Runtime = Runtime.DOTNET_6,
                Architecture = Architecture.X86_64,
                Name = "E2ETestLambda_X86_NET6",
                Handler = "LoggingFunction::LoggingFunction.Function::FunctionHandler",
                SourcePath = "../lambdas/LoggingFunction/src/LoggingFunction",
                DistPath = "../lambdas/LoggingFunction/dist"
            });
            new FunctionConstruct(this, "lambda_arm_net6", new FunctionConstructProps
            {
                Runtime = Runtime.DOTNET_6,
                Architecture = Architecture.ARM_64,
                Name = "E2ETestLambda_ARM_NET6",
                Handler = "LoggingFunction::LoggingFunction.Function::FunctionHandler",
                SourcePath = "../lambdas/LoggingFunction/src/LoggingFunction",
                DistPath = "../lambdas/LoggingFunction/dist"
            });
            // var lambda = new Function(this, "Lambda", new FunctionProps
            // {
            //     Runtime = Runtime.DOTNET_8,
            //     Architecture = Architecture.X86_64,
            //     FunctionName = "E2ETestLambda_X86_NET8",
            //     Handler = "LoggingFunction::LoggingFunction.Function::FunctionHandler",
            //     Code = Code.FromCustomCommand("../lambdas/LoggingFunction/dist",
            //         [
            //             "dotnet-lambda package -pl ../lambdas/LoggingFunction/src/LoggingFunction -o ../lambdas/LoggingFunction/dist"
            //         ],
            //         new CustomCommandOptions
            //         {
            //             CommandOptions = new Dictionary<string, object> {{"shell", true }}
            //         })
            // });
            //
            // var lambdaARM = new Function(this, "Lambda", new FunctionProps
            // {
            //     Runtime = Runtime.DOTNET_8,
            //     Architecture = Architecture.ARM_64,
            //     FunctionName = "E2ETestLambda_ARM_NET8",
            //     Handler = "LoggingFunction::LoggingFunction.Function::FunctionHandler",
            //     Code = Code.FromCustomCommand("../lambdas/LoggingFunction/dist",
            //         [
            //             "dotnet-lambda package -pl ../lambdas/LoggingFunction/src/LoggingFunction -o ../lambdas/LoggingFunction/dist"
            //         ],
            //         new CustomCommandOptions
            //         {
            //             CommandOptions = new Dictionary<string, object> {{"shell", true }}
            //         })
            // });
        }
    }
}

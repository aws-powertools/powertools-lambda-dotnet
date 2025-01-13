using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using Architecture = Amazon.CDK.AWS.Lambda.Architecture;

namespace Infra
{
    public class CoreStack : Stack
    {
        internal CoreStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            CreateFunctionConstructs("logging");
            CreateFunctionConstructs("metrics");
            CreateFunctionConstructs("tracing");
        }

        private void CreateFunctionConstructs(string utility)
        {
            var basePath = $"../functions/core/{utility}/Function/src/Function";
            var distPath = $"../functions/core/{utility}/Function/dist";

            CreateFunctionConstruct(this, $"{utility}_X64_net8", Runtime.DOTNET_8, Architecture.X86_64,
                $"E2ETestLambda_X64_NET8_{utility}", basePath, distPath);
            CreateFunctionConstruct(this, $"{utility}_arm_net8", Runtime.DOTNET_8, Architecture.ARM_64,
                $"E2ETestLambda_ARM_NET8_{utility}", basePath, distPath);
            CreateFunctionConstruct(this, $"{utility}_X64_net6", Runtime.DOTNET_6, Architecture.X86_64,
                $"E2ETestLambda_X64_NET6_{utility}", basePath, distPath);
            CreateFunctionConstruct(this, $"{utility}_arm_net6", Runtime.DOTNET_6, Architecture.ARM_64,
                $"E2ETestLambda_ARM_NET6_{utility}", basePath, distPath);
        }

        private void CreateFunctionConstruct(Construct scope, string id, Runtime runtime, Architecture architecture,
            string name, string sourcePath, string distPath)
        {
            _  = new FunctionConstruct(scope, id, new FunctionConstructProps
            {
                Runtime = runtime,
                Architecture = architecture,
                Name = name,
                Handler = "Function::Function.Function::FunctionHandler",
                SourcePath = sourcePath,
                DistPath = distPath
            });
        }
    }
}
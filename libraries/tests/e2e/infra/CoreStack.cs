using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using InfraShared;
using Architecture = Amazon.CDK.AWS.Lambda.Architecture;

namespace Infra
{
    public class ConstructArgs
    {
        public ConstructArgs(Construct scope, string id, Runtime runtime, Architecture architecture, string name, string sourcePath, string distPath)
        {
            Scope = scope;
            Id = id;
            Runtime = runtime;
            Architecture = architecture;
            Name = name;
            SourcePath = sourcePath;
            DistPath = distPath;
        }

        public Construct Scope { get; private set; }
        public string Id { get; private set; }
        public Runtime Runtime { get; private set; }
        public Architecture Architecture { get; private set; }
        public string Name { get; private set; }
        public string SourcePath { get; private set; }
        public string DistPath { get; private set; }
    }

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

            CreateFunctionConstruct(new ConstructArgs(this, $"{utility}_X64_net8", Runtime.DOTNET_8, Architecture.X86_64, $"E2ETestLambda_X64_NET8_{utility}", basePath, distPath));
            CreateFunctionConstruct(new ConstructArgs(this, $"{utility}_arm_net8", Runtime.DOTNET_8, Architecture.ARM_64, $"E2ETestLambda_ARM_NET8_{utility}", basePath, distPath));
            CreateFunctionConstruct(new ConstructArgs(this, $"{utility}_X64_net6", Runtime.DOTNET_6, Architecture.X86_64, $"E2ETestLambda_X64_NET6_{utility}", basePath, distPath));
            CreateFunctionConstruct(new ConstructArgs(this, $"{utility}_arm_net6", Runtime.DOTNET_6, Architecture.ARM_64, $"E2ETestLambda_ARM_NET6_{utility}", basePath, distPath));
        }

        private void CreateFunctionConstruct(ConstructArgs constructArgs)
        {
            _  = new FunctionConstruct(constructArgs.Scope, constructArgs.Id, new FunctionConstructProps
            {
                Runtime = constructArgs.Runtime,
                Architecture = constructArgs.Architecture,
                Name = constructArgs.Name,
                Handler = "Function::Function.Function::FunctionHandler",
                SourcePath = constructArgs.SourcePath,
                DistPath = constructArgs.DistPath,
            });
        }
    }
}
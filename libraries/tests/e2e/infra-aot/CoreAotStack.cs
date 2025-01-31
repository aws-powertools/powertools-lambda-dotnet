using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using InfraShared;
using Architecture = Amazon.CDK.AWS.Lambda.Architecture;

namespace InfraAot;

public class CoreAotStack : Stack
{
    private readonly Architecture _architecture;

    internal CoreAotStack(Construct scope, string id, PowertoolsDefaultStackProps props = null) : base(scope, id, props)
    {
        if (props != null) _architecture = props.ArchitectureString == "arm64" ? Architecture.ARM_64 : Architecture.X86_64;

        CreateFunctionConstructs("logging");
        CreateFunctionConstructs("metrics");
        CreateFunctionConstructs("tracing");
    }

    private void CreateFunctionConstructs(string utility)
    {
        var baseAotPath = $"../functions/core/{utility}/AOT-Function/src/AOT-Function";
        var distAotPath = $"../functions/core/{utility}/AOT-Function/dist";
        var arch = _architecture == Architecture.X86_64 ? "X64" : "ARM";

        CreateFunctionConstruct(this, $"{utility}_{arch}_aot_net8", Runtime.DOTNET_8, _architecture,
            $"E2ETestLambda_{arch}_AOT_NET8_{utility}", baseAotPath, distAotPath);
    }

    private void CreateFunctionConstruct(Construct scope, string id, Runtime runtime, Architecture architecture,
        string name, string sourcePath, string distPath)
    {
        _ = new FunctionConstruct(scope, id, new FunctionConstructProps
        {
            Runtime = runtime,
            Architecture = architecture,
            Name = name,
            Handler = "AOT-Function",
            SourcePath = sourcePath,
            DistPath = distPath,
            IsAot = true
        });
    }
}
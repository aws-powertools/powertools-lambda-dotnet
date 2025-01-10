using System.Runtime.InteropServices;
using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using Architecture = Amazon.CDK.AWS.Lambda.Architecture;

namespace InfraAot;

public class CoreAotStack : Stack
{
    internal CoreAotStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        CreateFunctionConstructs("logging");
        CreateFunctionConstructs("metrics");
        CreateFunctionConstructs("tracing");
    }

    private void CreateFunctionConstructs(string utility)
    {
        string baseAotPath = $"../functions/core/{utility}/AOT-Function/src/AOT-Function";
        string distAotPath = $"../functions/core/{utility}/AOT-Function/dist";

        if (RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
        {
            CreateFunctionConstruct(this, $"{utility}_ARM_aot_net8", Runtime.DOTNET_8, Architecture.ARM_64, $"E2ETestLambda_ARM_AOT_NET8_{utility}", baseAotPath, distAotPath);
        }

        if (RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X64)
        {
            CreateFunctionConstruct(this, $"{utility}_X64_aot_net8", Runtime.DOTNET_8, Architecture.X86_64, $"E2ETestLambda_X64_AOT_NET8_{utility}", baseAotPath, distAotPath);
        }
    }

    private void CreateFunctionConstruct(Construct scope, string id, Runtime runtime, Architecture architecture, string name, string sourcePath, string distPath)
    {
        new FunctionConstruct(scope, id, new FunctionConstructProps
        {
            Runtime = runtime,
            Architecture = architecture,
            Name = name,
            Handler = "AOT-Function",
            SourcePath = sourcePath,
            DistPath = distPath
        });
    }
}
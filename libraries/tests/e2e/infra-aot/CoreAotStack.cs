using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using InfraShared;
using Architecture = Amazon.CDK.AWS.Lambda.Architecture;

namespace InfraAot;

public class ConstructArgs
{
    public ConstructArgs(Construct scope, string id, Runtime runtime, Architecture architecture, string name, string sourcePath, string distPath, string handler)
    {
        Scope = scope;
        Id = id;
        Runtime = runtime;
        Architecture = architecture;
        Name = name;
        SourcePath = sourcePath;
        DistPath = distPath;
        Handler = handler;
    }

    public Construct Scope { get; private set; }
    public string Id { get; private set; }
    public Runtime Runtime { get; private set; }
    public Architecture Architecture { get; private set; }
    public string Name { get; private set; }
    public string SourcePath { get; private set; }
    public string DistPath { get; private set; }
    public string Handler { get; private set; }
}

public class CoreAotStack : Stack
{
    private readonly Architecture _architecture;

    internal CoreAotStack(Construct scope, string id, PowertoolsDefaultStackProps props = null) : base(scope, id, props)
    {
        if (props != null) _architecture = props.ArchitectureString == "arm64" ? Architecture.ARM_64 : Architecture.X86_64;

        CreateFunctionConstructs("logging");
        CreateFunctionConstructs("logging", "AOT-Function-ILogger");
        CreateFunctionConstructs("metrics");
        CreateFunctionConstructs("tracing");
    }

    private void CreateFunctionConstructs(string utility, string function = "AOT-Function" )
    {
        var baseAotPath = $"../functions/core/{utility}/{function}/src/{function}";
        var distAotPath = $"../functions/core/{utility}/{function}/dist/{function}";
        var arch = _architecture == Architecture.X86_64 ? "X64" : "ARM";

        CreateFunctionConstruct(new ConstructArgs(this, $"{utility}_{arch}_aot_net8_{function}", Runtime.DOTNET_8, _architecture, $"E2ETestLambda_{arch}_AOT_NET8_{utility}_{function}", baseAotPath, distAotPath, function));
    }

    private void CreateFunctionConstruct(ConstructArgs constructArgs)
    {
        _ = new FunctionConstruct(constructArgs.Scope, constructArgs.Id, new FunctionConstructProps
        {
            Runtime = constructArgs.Runtime,
            Architecture = constructArgs.Architecture,
            Name = constructArgs.Name,
            Handler = constructArgs.Handler,
            SourcePath = constructArgs.SourcePath,
            DistPath = constructArgs.DistPath,
            IsAot = true
        });
    }
}
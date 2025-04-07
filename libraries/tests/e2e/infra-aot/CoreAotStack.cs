using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using InfraShared;
using Architecture = Amazon.CDK.AWS.Lambda.Architecture;

namespace InfraAot;

public class ConstructArgs
{
    public Construct Scope { get; set; }
    public string Id { get; set; }
    public Runtime Runtime { get; set; }
    public Architecture Architecture { get; set; }
    public string Name { get; set; }
    public string SourcePath { get; set; }
    public string DistPath { get; set; }
    public string Handler { get; set; }
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

        var construct = new ConstructArgs
        {
            Scope = this,
            Id = $"{utility}_{arch}_aot_net8_{function}",
            Runtime = Runtime.DOTNET_8,
            Architecture = _architecture,
            Name = $"E2ETestLambda_{arch}_AOT_NET8_{utility}_{function}",
            SourcePath = baseAotPath,
            DistPath = distAotPath,
            Handler = $"{function}.Function::AWS.Lambda.Powertools.{utility}.{function}.Function.FunctionHandler"
        };
        
        CreateFunctionConstruct(construct);
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
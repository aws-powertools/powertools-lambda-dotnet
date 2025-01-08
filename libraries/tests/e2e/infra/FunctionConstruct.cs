using System.Collections.Generic;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace E2E;

public class FunctionConstructProps
{
    public Architecture Architecture;
    public Runtime Runtime;
    public string Name;
    public string Handler;
    public string SourcePath;
    public string DistPath;
    public bool IsAot;
}

public class FunctionConstruct : Construct
{
    public FunctionConstruct(Construct scope, string id, FunctionConstructProps props) : base(scope, id)
    {
        var framework = props.Runtime == Runtime.DOTNET_6 ? "net6.0" : "net8.0";
        var distPath = $"{props.DistPath}/deploy_{props.Architecture.Name}_{props.Runtime.Name}.zip";
        var lambda = new Function(this, id, new FunctionProps
        {
            Runtime = props.Runtime,
            Architecture = props.Architecture,
            FunctionName = props.Name,
            Handler = props.Handler,
            Code = Code.FromCustomCommand(distPath,
                [
                    $"dotnet-lambda package -pl {props.SourcePath} -o {distPath} -f {framework} -farch {props.Architecture.Name} {(props.IsAot ? "-cifb public.ecr.aws/sam/build-dotnet8" : "")}"
                ],
                new CustomCommandOptions
                {
                    CommandOptions = new Dictionary<string, object> {{"shell", true }}
                })
        });
    }
}
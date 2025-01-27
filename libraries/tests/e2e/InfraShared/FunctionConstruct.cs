using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace InfraShared;

public class FunctionConstruct : Construct
{
    public Function Function { get; set; }
    
    public FunctionConstruct(Construct scope, string id, FunctionConstructProps props) : base(scope, id)
    {
        var framework = props.Runtime == Runtime.DOTNET_6 ? "net6.0" : "net8.0";
        var distPath = $"{props.DistPath}/deploy_{props.Architecture.Name}_{props.Runtime.Name}.zip";
        var command = props.IsAot
            ? $"dotnet-lambda package -pl {props.SourcePath} -cmd ../../../ -o {distPath} -f net8.0 -farch {props.Architecture.Name} -cifb public.ecr.aws/sam/build-dotnet8"
            : $"dotnet-lambda package -pl {props.SourcePath} -o {distPath} -f {framework} -farch {props.Architecture.Name}";

        Function = new Function(this, id, new FunctionProps
        {
            Runtime = props.Runtime,
            Architecture = props.Architecture,
            FunctionName = props.Name,
            Handler = props.Handler,
            Tracing = Tracing.ACTIVE,
            Timeout = Duration.Seconds(10),
            Code = Code.FromCustomCommand(distPath,
                new[]
                {
                    command
                },
                new CustomCommandOptions
                {
                    CommandOptions = new Dictionary<string, object> { { "shell", true } }
                })
        });
    }

    
    // public FunctionConstruct(Construct scope, string id, FunctionConstructProps props) : base(scope, id)
    // {
    //     if(props.IsAot)
    //     {
    //         var distPath = $"{props.DistPath}/deploy_{props.Architecture.Name}_{props.Runtime.Name}.zip";
    //         _ = new Function(this, id, new FunctionProps
    //         {
    //             Runtime = props.Runtime,
    //             Architecture = props.Architecture,
    //             FunctionName = props.Name,
    //             Handler = props.Handler,
    //             Tracing = Tracing.ACTIVE,
    //             Timeout = Duration.Seconds(10),
    //             Code = Code.FromCustomCommand(distPath,
    //                 [
    //                     $"dotnet-lambda package -pl {props.SourcePath} -cmd ../../../ -o {distPath} -f net8.0 -farch {props.Architecture.Name} -cifb public.ecr.aws/sam/build-dotnet8"
    //                 ],
    //                 new CustomCommandOptions
    //                 {
    //                     CommandOptions = new Dictionary<string, object> {{"shell", true }}
    //                 })
    //         });
    //     }
    //     else
    //     {
    //         var framework = props.Runtime == Runtime.DOTNET_6 ? "net6.0" : "net8.0";
    //         var distPath = $"{props.DistPath}/deploy_{props.Architecture.Name}_{props.Runtime.Name}.zip";
    //         _ = new Function(this, id, new FunctionProps
    //         {
    //             Runtime = props.Runtime,
    //             Architecture = props.Architecture,
    //             FunctionName = props.Name,
    //             Handler = props.Handler,
    //             Tracing = Tracing.ACTIVE,
    //             Timeout = Duration.Seconds(10),
    //             Code = Code.FromCustomCommand(distPath,
    //                 [
    //                     $"dotnet-lambda package -pl {props.SourcePath} -o {distPath} -f {framework} -farch {props.Architecture.Name}"
    //                 ],
    //                 new CustomCommandOptions
    //                 {
    //                     CommandOptions = new Dictionary<string, object> { { "shell", true } }
    //                 })
    //         });
    //     }
    // }
}
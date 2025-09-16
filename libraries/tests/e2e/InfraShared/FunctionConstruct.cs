using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace InfraShared;

public class FunctionConstruct : Construct
{
    public Function Function { get; set; }

    public FunctionConstruct(Construct scope, string id, FunctionConstructProps props) : base(scope, id)
    {
        var framework = "net8.0";
        var distPath = $"{props.DistPath}/deploy_{props.Architecture.Name}_{props.Runtime.Name}.zip";
        var command = props.IsAot
            ? $"dotnet-lambda package -pl {props.SourcePath} -cmd ../../../ -o {distPath} -farch {props.Architecture.Name} -cifb public.ecr.aws/sam/build-dotnet8"
            : $"dotnet-lambda package -pl {props.SourcePath} -o {distPath} -f {framework} -farch {props.Architecture.Name}";
        
        Console.WriteLine(command);

        Function = new Function(this, id, new FunctionProps
        {
            Runtime = props.Runtime,
            Architecture = props.Architecture,
            FunctionName = props.Name,
            Handler = props.Handler!,
            Tracing = Tracing.ACTIVE,
            Timeout = Duration.Seconds(10),
            Environment = props.Environment,
            LoggingFormat = LoggingFormat.TEXT,
            Code = Code.FromCustomCommand(distPath,
                [
                    command
                ],
                new CustomCommandOptions
                {
                    CommandOptions = new Dictionary<string, object> { { "shell", true } }
                })
        });
    }
}
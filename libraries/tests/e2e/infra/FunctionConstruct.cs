// using System.Collections.Generic;
// using Amazon.CDK;
// using Amazon.CDK.AWS.Lambda;
// using Constructs;
// using InfraShared;
//
// namespace Infra;
//
// public class FunctionConstruct : Construct
// {
//     public FunctionConstruct(Construct scope, string id, FunctionConstructProps props) : base(scope, id)
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
//                     CommandOptions = new Dictionary<string, object> {{"shell", true }}
//                 })
//         });
//     }
// }
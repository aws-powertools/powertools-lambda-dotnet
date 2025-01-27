// using System.Collections.Generic;
// using Amazon.CDK;
// using Amazon.CDK.AWS.Lambda;
// using Constructs;
// using InfraShared;
//
// namespace InfraAot;
//
// public class FunctionConstruct : Construct
// {
//     public FunctionConstruct(Construct scope, string id, FunctionConstructProps props) : base(scope, id)
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
// }
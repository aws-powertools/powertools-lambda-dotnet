using System.Runtime.InteropServices;
using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using Architecture = Amazon.CDK.AWS.Lambda.Architecture;

namespace E2E
{
    public class E2EStack : Stack
    {
        internal E2EStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            CreateFunctionConstructs("logging");
            CreateFunctionConstructs("metrics");
            CreateFunctionConstructs("tracing");
            
            // new FunctionConstruct(this, "lambda_X64_net8", new FunctionConstructProps
            // {
            //     Runtime = Runtime.DOTNET_8,
            //     Architecture = Architecture.X86_64,
            //     Name = "E2ETestLambda_X64_NET8",
            //     Handler = "Function::Function.Function::FunctionHandler",
            //     SourcePath = "../functions/core/Function/src/Function",
            //     DistPath = "../functions/core/Function/dist"
            // });
            // new FunctionConstruct(this, "lambda_arm_net8", new FunctionConstructProps
            // {
            //     Runtime = Runtime.DOTNET_8,
            //     Architecture = Architecture.ARM_64,
            //     Name = "E2ETestLambda_ARM_NET8",
            //     Handler = "Function::Function.Function::FunctionHandler",
            //     SourcePath = "../functions/core/Function/src/Function",
            //     DistPath = "../functions/core/Function/dist"
            // });
            // new FunctionConstruct(this, "lambda_X64_net6", new FunctionConstructProps
            // {
            //     Runtime = Runtime.DOTNET_6,
            //     Architecture = Architecture.X86_64,
            //     Name = "E2ETestLambda_X64_NET6",
            //     Handler = "Function::Function.Function::FunctionHandler",
            //     SourcePath = "../functions/core/Function/src/Function",
            //     DistPath = "../functions/core/Function/dist"
            // });
            // new FunctionConstruct(this, "lambda_arm_net6", new FunctionConstructProps
            // {
            //     Runtime = Runtime.DOTNET_6,
            //     Architecture = Architecture.ARM_64,
            //     Name = "E2ETestLambda_ARM_NET6",
            //     Handler = "Function::Function.Function::FunctionHandler",
            //     SourcePath = "../functions/core/Function/src/Function",
            //     DistPath = "../functions/core/Function/dist"
            // });
            //
            // // AOT
            //
            // if (RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
            // {
            //     new FunctionConstruct(this, "lambda_ARM_aot_net8", new FunctionConstructProps
            //     {
            //         Runtime = Runtime.DOTNET_8,
            //         Architecture = Architecture.ARM_64,
            //         Name = "E2ETestLambda_ARM_AOT_NET8",
            //         Handler = "AOT-Function",
            //         SourcePath = "../functions/core/AOT-Function/src/AOT-Function",
            //         DistPath = "../functions/core/AOT-Function/dist",
            //         UseContainerForBuild = true
            //     });
            // }
            //
            // if (RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X64)
            // {
            //     new FunctionConstruct(this, "lambda_ARM_X64_net8", new FunctionConstructProps
            //     {
            //         Runtime = Runtime.DOTNET_8,
            //         Architecture = Architecture.X86_64,
            //         Name = "E2ETestLambda_X64_AOT_NET8",
            //         Handler = "AOT-Function",
            //         SourcePath = "../functions/core/AOT-Function/src/AOT-Function",
            //         DistPath = "../functions/core/AOT-Function/dist",
            //         UseContainerForBuild = true
            //     });
            // }
        }
        
        private void CreateFunctionConstructs(string utility)
        {
            string basePath = $"../functions/core/{utility}/Function/src/Function";
            string distPath = $"../functions/core/{utility}/Function/dist";
            string baseAotPath = $"../functions/core/{utility}/AOT-Function/src/AOT-Function";
            string distAotPath = $"../functions/core/{utility}/AOT-Function/dist";

            CreateFunctionConstruct(this, $"{utility}_X64_net8", Runtime.DOTNET_8, Architecture.X86_64, $"E2ETestLambda_X64_NET8_{utility}", basePath, distPath);
            CreateFunctionConstruct(this, $"{utility}_arm_net8", Runtime.DOTNET_8, Architecture.ARM_64, $"E2ETestLambda_ARM_NET8_{utility}", basePath, distPath);
            CreateFunctionConstruct(this, $"{utility}_X64_net6", Runtime.DOTNET_6, Architecture.X86_64, $"E2ETestLambda_X64_NET6_{utility}", basePath, distPath);
            CreateFunctionConstruct(this, $"{utility}_arm_net6", Runtime.DOTNET_6, Architecture.ARM_64, $"E2ETestLambda_ARM_NET6_{utility}", basePath, distPath);

            if (RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
            {
                CreateFunctionConstruct(this, $"{utility}_ARM_aot_net8", Runtime.DOTNET_8, Architecture.ARM_64, $"E2ETestLambda_ARM_AOT_NET8_{utility}", baseAotPath, distAotPath, true);
            }

            if (RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X64)
            {
                CreateFunctionConstruct(this, $"{utility}_X64_aot_net8", Runtime.DOTNET_8, Architecture.X86_64, $"E2ETestLambda_X64_AOT_NET8_{utility}", baseAotPath, distAotPath, true);
            }
        }
        
        private void CreateFunctionConstruct(Construct scope, string id, Runtime runtime, Architecture architecture, string name, string sourcePath, string distPath, bool isAot = false)
        {
            new FunctionConstruct(scope, id, new FunctionConstructProps
            {
                Runtime = runtime,
                Architecture = architecture,
                Name = name,
                Handler = isAot ? "AOT-Function" : "Function::Function.Function::FunctionHandler",
                SourcePath = sourcePath,
                DistPath = distPath,
                IsAot = isAot
            });
        }
    }
}
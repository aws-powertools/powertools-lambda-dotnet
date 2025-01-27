using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace InfraShared;

public class IdempotencyStackProps : IStackProps
{
    public required Runtime Runtime { get; set; }
    public required string Name { get; set; }
    public required string Handler { get; set; }
}

public class IdempotencyStack : Stack
{
    public IdempotencyStack(Construct scope, string id, IdempotencyStackProps props) : base(scope, id, props)
    {
        Table = new Table(this, "Idempotency", new TableProps
        {
            PartitionKey = new Attribute
            {
                Name = "Id",
                Type = AttributeType.STRING
            },
            TableName = "IdempotencyTable",
            BillingMode = BillingMode.PAY_PER_REQUEST,
            TimeToLiveAttribute = "expiration"
        });
        
        var utility = "Idempotency";
        var basePath = $"../functions/core/{utility}/Function/src/Function";
        var distPath = $"../functions/core/{utility}/Function/dist";

        CreateFunctionConstruct(this, $"{utility}_X64_net8", Runtime.DOTNET_8, Architecture.X86_64,
            $"E2ETestLambda_X64_NET8_{utility}", basePath, distPath);
        CreateFunctionConstruct(this, $"{utility}_arm_net8", Runtime.DOTNET_8, Architecture.ARM_64,
            $"E2ETestLambda_ARM_NET8_{utility}", basePath, distPath);
        CreateFunctionConstruct(this, $"{utility}_X64_net6", Runtime.DOTNET_6, Architecture.X86_64,
            $"E2ETestLambda_X64_NET6_{utility}", basePath, distPath);
        CreateFunctionConstruct(this, $"{utility}_arm_net6", Runtime.DOTNET_6, Architecture.ARM_64,
            $"E2ETestLambda_ARM_NET6_{utility}", basePath, distPath);
    }

    public Table Table { get; set; }

    private void CreateFunctionConstruct(Construct scope, string id, Runtime runtime, Architecture architecture,
        string name, string sourcePath, string distPath)
    {
        var lambdaFunction = new FunctionConstruct(scope, id, new FunctionConstructProps
        {
            Runtime = runtime,
            Architecture = architecture,
            Name = name,
            Handler = "AOT-Function",
            SourcePath = sourcePath,
            DistPath = distPath,
            IsAot = true
        });

        // var lambdaFunction = new Function(this, "IdempotencyFunction", new FunctionProps
        // {
        //     Runtime = props.Runtime,
        //     Handler = props.Handler,
        //     Code = Code.FromAsset("path/to/your/lambda/code"),
        //     Environment = new Dictionary<string, string>
        //     {
        //         { "TABLE_NAME", table.TableName }
        //     }
        // });

        Table.GrantReadWriteData(lambdaFunction.Function);
    }
}

using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace InfraShared;

public class IdempotencyStack : Stack
{
    public Table Table { get; set; }
    
    public IdempotencyStack(Construct scope, string id, PowertoolsDefaultStackProps props) : base(scope, id, props)
    {
        Table = new Table(this, "Idempotency", new TableProps
        {
            PartitionKey = new Attribute
            {
                Name = "id",
                Type = AttributeType.STRING
            },
            TableName = "IdempotencyTable",
            BillingMode = BillingMode.PAY_PER_REQUEST,
            TimeToLiveAttribute = "expiration",
            DeletionProtection = false
        });
        
        var utility = "idempotency";
        var basePath = $"../functions/{utility}/Function/src/Function";
        var distPath = $"../functions/{utility}/Function/dist";

        CreateFunctionConstruct(this, $"{utility}_X64_net8", Runtime.DOTNET_8, Architecture.X86_64,
            $"E2ETestLambda_X64_NET8_{utility}", basePath, distPath, props);
        CreateFunctionConstruct(this, $"{utility}_arm_net8", Runtime.DOTNET_8, Architecture.ARM_64,
            $"E2ETestLambda_ARM_NET8_{utility}", basePath, distPath, props);
        CreateFunctionConstruct(this, $"{utility}_X64_net6", Runtime.DOTNET_6, Architecture.X86_64,
            $"E2ETestLambda_X64_NET6_{utility}", basePath, distPath, props);
        CreateFunctionConstruct(this, $"{utility}_arm_net6", Runtime.DOTNET_6, Architecture.ARM_64,
            $"E2ETestLambda_ARM_NET6_{utility}", basePath, distPath, props);
    }

    private void CreateFunctionConstruct(Construct scope, string id, Runtime runtime, Architecture architecture,
        string name, string sourcePath, string distPath, PowertoolsDefaultStackProps props)
    {
        var lambdaFunction = new FunctionConstruct(scope, id, new FunctionConstructProps
        {
            Runtime = runtime,
            Architecture = architecture,
            Name = name,
            Handler = "Function::Function.Function::FunctionHandler",
            SourcePath = sourcePath,
            DistPath = distPath,
            Environment = new Dictionary<string, string>
            {
                { "IDEMPOTENCY_TABLE_NAME", Table.TableName }
            },
            IsAot = props.IsAot
        });
        
        // Grant the Lambda function permissions to perform all actions on the DynamoDB table
        Table.GrantReadWriteData(lambdaFunction.Function);
    }
}

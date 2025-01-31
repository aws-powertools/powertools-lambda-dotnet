using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace InfraShared;

public class IdempotencyStack : Stack
{
    public Table Table { get; set; }

    public IdempotencyStack(Construct scope, string id, IdempotencyStackProps props) : base(scope, id, props)
    {
        Table = new Table(this, "Idempotency", new TableProps
        {
            PartitionKey = new Attribute
            {
                Name = "id",
                Type = AttributeType.STRING
            },
            TableName = props.TableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            TimeToLiveAttribute = "expiration",
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var utility = "idempotency";

        if (props.IsAot)
        {
            var baseAotPath = $"../functions/{utility}/AOT-Function/src/AOT-Function";
            var distAotPath = $"../functions/{utility}/AOT-Function/dist";
            var path = new Path(baseAotPath, distAotPath);
            
            var architecture = props.ArchitectureString == "arm64" ? Architecture.ARM_64 : Architecture.X86_64;
            var arch = architecture == Architecture.X86_64 ? "X64" : "ARM";
            CreateFunctionConstruct(this, $"{utility}_{arch}_aot_net8", Runtime.DOTNET_8, architecture,
                $"E2ETestLambda_{arch}_AOT_NET8_{utility}", path, props);
        }
        else
        {
            var basePath = $"../functions/{utility}/Function/src/Function";
            var distPath = $"../functions/{utility}/Function/dist";
            var path = new Path(basePath, distPath);
            
            CreateFunctionConstruct(this, $"{utility}_X64_net8", Runtime.DOTNET_8, Architecture.X86_64,
                $"E2ETestLambda_X64_NET8_{utility}", path, props);
            CreateFunctionConstruct(this, $"{utility}_arm_net8", Runtime.DOTNET_8, Architecture.ARM_64,
                $"E2ETestLambda_ARM_NET8_{utility}", path, props);
            CreateFunctionConstruct(this, $"{utility}_X64_net6", Runtime.DOTNET_6, Architecture.X86_64,
                $"E2ETestLambda_X64_NET6_{utility}", path, props);
            CreateFunctionConstruct(this, $"{utility}_arm_net6", Runtime.DOTNET_6, Architecture.ARM_64,
                $"E2ETestLambda_ARM_NET6_{utility}", path, props);
        }
    }

    private void CreateFunctionConstruct(Construct scope, string id, Runtime runtime, Architecture architecture,
        string name,Path path, PowertoolsDefaultStackProps props)
    {
        var lambdaFunction = new FunctionConstruct(scope, id, new FunctionConstructProps
        {
            Runtime = runtime,
            Architecture = architecture,
            Name = name,
            Handler = props.IsAot ? "AOT-Function" : "Function::Function.Function::FunctionHandler",
            SourcePath = path.SourcePath,
            DistPath = path.DistPath,
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

public record Path(string SourcePath, string DistPath);

using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.Kinesis;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;
using Stream = Amazon.CDK.AWS.Kinesis.Stream;

namespace InfraShared;

public class BatchProcessingStackProps : PowertoolsDefaultStackProps
{
    // Any specific properties for batch processing
}

public class BatchProcessingStack : Stack
{
    public Table DynamoDbTable { get; set; }
    public Queue StandardQueue { get; set; }
    public Queue FifoQueue { get; set; }
    public Stream KinesisStream { get; set; }

    public BatchProcessingStack(Construct scope, string id, BatchProcessingStackProps props) : base(scope, id, props)
    {
        // DynamoDB table with streams enabled
        DynamoDbTable = new Table(this, "BatchProcessingTable", new TableProps
        {
            PartitionKey = new Attribute
            {
                Name = "id",
                Type = AttributeType.STRING
            },
            TableName = "BatchProcessingTable",
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.DESTROY,
            Stream = StreamViewType.NEW_AND_OLD_IMAGES
        });

        // SQS Queues
        StandardQueue = new Queue(this, "BatchProcessingStandardQueue", new QueueProps
        {
            QueueName = "BatchProcessingStandardQueue",
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        FifoQueue = new Queue(this, "BatchProcessingFifoQueue", new QueueProps
        {
            QueueName = "BatchProcessingFifoQueue.fifo",
            Fifo = true,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        // Kinesis Data Stream
        KinesisStream = new Stream(this, "BatchProcessingKinesisStream", new StreamProps
        {
            StreamName = "BatchProcessingKinesisStream",
            ShardCount = 1,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var utility = "batchprocessing";

        if (props.IsAot)
        {
            CreateAotFunctions(this, utility, props);
        }
        else
        {
            CreateRegularFunctions(this, utility, props);
        }
    }

    private void CreateAotFunctions(Construct scope, string utility, BatchProcessingStackProps props)
    {
        var sources = new[] { "DynamoDB", "SQS", "Kinesis" };
        
        foreach (var source in sources)
        {
            var baseAotPath = $"../functions/{utility}/AOT-Function/src/AOT-Function{source}";
            var distAotPath = $"../functions/{utility}/AOT-Function/dist/AOT-Function{source}";
            var path = new Path(baseAotPath, distAotPath);
            props.Handler = $"AOT-Function{source}";

            var architecture = props.ArchitectureString == "arm64" ? Architecture.ARM_64 : Architecture.X86_64;
            var arch = architecture == Architecture.X86_64 ? "X64" : "ARM";
            
            var lambdaFunction = CreateFunctionConstruct(
                scope, 
                $"{utility}_{arch}_aot_net8__{source}", 
                Runtime.DOTNET_8, 
                architecture,
                $"E2ETestLambda_{arch}_AOT_NET8_{utility}_{source}", 
                path, 
                props
            );
            
            ConfigureEventSource(lambdaFunction, source);
        }
    }

    private void CreateRegularFunctions(Construct scope, string utility, BatchProcessingStackProps props)
    {
        var sources = new[] { "DynamoDB", "SQS", "Kinesis" };
        
        foreach (var source in sources)
        {
            var basePath = $"../functions/{utility}/Function/src/Function{source}";
            var distPath = $"../functions/{utility}/Function/dist/Function{source}";
            var path = new Path(basePath, distPath);
            props.Handler = $"Function{source}::Function{source}.Function::FunctionHandler";

            // Create Lambda functions for different runtimes and architectures
            var runtimes = new[] { 
                (runtime: Runtime.DOTNET_8, arch: Architecture.X86_64, archStr: "X64", runtimeStr : "NET8"),
                (runtime: Runtime.DOTNET_8, arch: Architecture.ARM_64, archStr: "ARM", runtimeStr : "NET8"),
                (runtime: Runtime.DOTNET_6, arch: Architecture.X86_64, archStr: "X64", runtimeStr : "NET6"),
                (runtime: Runtime.DOTNET_6, arch: Architecture.ARM_64, archStr: "ARM", runtimeStr : "NET6")
            };

            foreach (var (runtime, arch, archStr, runtimeStr) in runtimes)
            {
                var lambdaFunction = CreateFunctionConstruct(
                    scope,
                    $"{utility}_{archStr}_{runtimeStr}_{source}",
                    runtime,
                    arch,
                    $"E2ETestLambda_{archStr}_{runtimeStr}_{utility}_{source}",
                    path,
                    props
                );
                
                ConfigureEventSource(lambdaFunction, source);
            }
        }
    }

    private FunctionConstruct CreateFunctionConstruct(Construct scope, string id, Runtime runtime, Architecture architecture,
        string name, Path path, PowertoolsDefaultStackProps props)
    {
        var lambdaFunction = new FunctionConstruct(scope, id, new FunctionConstructProps
        {
            Runtime = runtime,
            Architecture = architecture,
            Name = name,
            Handler = props.Handler!,
            SourcePath = path.SourcePath,
            DistPath = path.DistPath,
            Environment = new Dictionary<string, string>
            {
                { "BATCH_PROCESSING_TABLE_NAME", DynamoDbTable.TableName },
                { "BATCH_PROCESSING_STANDARD_QUEUE_URL", StandardQueue.QueueUrl },
                { "BATCH_PROCESSING_FIFO_QUEUE_URL", FifoQueue.QueueUrl },
                { "BATCH_PROCESSING_KINESIS_STREAM_NAME", KinesisStream.StreamName }
            },
            IsAot = props.IsAot
        });

        return lambdaFunction;
    }

    private void ConfigureEventSource(FunctionConstruct lambdaFunction, string source)
    {
        switch (source)
        {
            case "DynamoDB":
                lambdaFunction.Function.AddEventSource(new DynamoEventSource(DynamoDbTable, new DynamoEventSourceProps
                {
                    StartingPosition = StartingPosition.LATEST,
                    BatchSize = 10,
                    RetryAttempts = 3
                }));
                break;
                
            case "SQS":
                lambdaFunction.Function.AddEventSource(new SqsEventSource(StandardQueue, new SqsEventSourceProps
                {
                    BatchSize = 10,
                    MaxBatchingWindow = Duration.Seconds(5)
                }));
                
                // Add permissions for FIFO queue too
                FifoQueue.GrantConsumeMessages(lambdaFunction.Function);
                break;
                
            case "Kinesis":
                lambdaFunction.Function.AddEventSource(new KinesisEventSource(KinesisStream, new KinesisEventSourceProps
                {
                    StartingPosition = StartingPosition.LATEST,
                    BatchSize = 10,
                    RetryAttempts = 3,
                    MaxBatchingWindow = Duration.Seconds(5)
                }));
                break;
        }
    }
}
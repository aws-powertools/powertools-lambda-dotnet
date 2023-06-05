# AWS Lambda Powertools for .NET - Web Api Example

This project contains the source code and supporting files for a web api hosted on AWS Lambda that you can deploy with the AWS Serverless Application Model Command Line Interface (AWS SAM CLI). It includes the following files and folders.

* src - Code for the application's Lambda function and Project Dockerfile.
* src/LambdaPowertoolsAPI/serverless.template -  A template that defines the application's AWS resources.
* events-  Invocation events that you can use to invoke the function.
* test - Unit tests for the application code.


The application uses several AWS resources, including Lambda functions and an API Gateway API. The project has been created using `Lambda ASP.NET Core Web API` [.NET Core project templates](https://github.com/aws/aws-lambda-dotnet/tree/master#amazonlambdaaspnetcoreserver). The `events` folder contains an exampleAPI Gateway proxy Lambda event and is not offered by the project template.

If you prefer to use an integrated development environment (IDE) to build and test your application, you can use the AWS Toolkit. The AWS Toolkit is an open source plug-in for popular IDEs that uses the AWS SAM CLI to build and deploy serverless applications on AWS. The AWS Toolkit also adds a simplified step-through debugging experience for Lambda function code. See the following links to get started.

* [Visual Studio Code](https://docs.aws.amazon.com/toolkit-for-vscode/latest/userguide/welcome.html)
* [Visual Studio](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/welcome.html)
* [Rider](https://docs.aws.amazon.com/toolkit-for-jetbrains/latest/userguide/welcome.html)


## Deploy the sample application

The AWS SAM CLI is an extension of the AWS Command Line Interface (AWS CLI) that adds functionality for building and testing Lambda applications. It uses Docker to run your functions in an Amazon Linux environment that matches Lambda. It can also emulate your application's build environment and API.

To use the AWS SAM CLI, you need the following tools.

* AWS SAM CLI - [Install the AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html)
* Docker - [Install Docker community edition](https://hub.docker.com/search/?type=edition&offering=community)

You will need the following for local testing.
* .NET 6.0 - [Install .NET 6.0](https://www.microsoft.com/net/download)

To build and deploy your application for the first time, run the following in your shell. Make sure the `serverless.template` file is in your current directory:

```bash
sam build -t ./serverless.template
sam deploy --guided
```

The first command will build a docker image from a Dockerfile and then copy the source of your application inside the Docker image. The second command will package and deploy your application to AWS, with a series of prompts:

* **Stack Name**: The name of the stack to deploy to CloudFormation. This should be unique to your account and region, and a good starting point would be something matching your project name. A rest of the document will assume it is `aws-lambda-powertools-web-api-sample`
* **AWS Region**: The AWS region you want to deploy your app to. The rest of the document will assume that stack name is `eu-central-1`
* **Confirm changes before deploy**: If set to yes, any change sets will be shown to you before execution for manual review. If set to no, the AWS SAM CLI will automatically deploy application changes.
* **Allow SAM CLI IAM role creation**: Many AWS SAM templates, including this example, create AWS IAM roles required for the AWS Lambda function(s) included to access AWS services. By default, these are scoped down to minimum required permissions. To deploy an AWS CloudFormation stack which creates or modifies IAM roles, the `CAPABILITY_IAM` value for `capabilities` must be provided. If permission isn't provided through this prompt, to deploy this example you must explicitly pass `--capabilities CAPABILITY_IAM` to the `sam deploy` command.
* **Save arguments to samconfig.toml**: If set to yes, your choices will be saved to a configuration file inside the project, so that in the future you can just re-run `sam deploy` without parameters to deploy changes to your application.

You can find your API Gateway Endpoint URL in the output values displayed after deployment. Remember that an attribute on `ValuesController` makes the resources available under `/api/values` so you need to at the end of the URL.

## Code Walkthrough

Once we have deployed the application on an AWS account we can execute below command to check if everything works. 

```bash
curl -i <url>/api/values
```

where `<url>` is an output form the `sam deploy` command. The result should be something like

```
HTTP/2 200 
content-type: application/json; charset=utf-8
content-length: 19
date: Sat, 27 May 2023 20:18:52 GMT
x-amzn-requestid: 1f6ec544-90ea-419f-b7a7-eed798bd5977
x-amz-apigw-id: FmTSnEH6FiAFnkQ=
x-amzn-trace-id: Root=1-647265aa-75513ad44dcd2e0f40ba1468;Sampled=1;lineage=383de489:0
x-cache: Miss from cloudfront
via: 1.1 d5bd9c82cbbad6f05501bb737b3688dc.cloudfront.net (CloudFront)
x-amz-cf-pop: WAW51-P3
x-amz-cf-id: X3PY9DHBUaduHJqjpOzk1KhrYVr6SoivVghabML4X_Rcq3AcX7uutA==

["value1","value2"]
```

If something went wrong you can use logs and tracing commands, described in one of the below sections, to debug. 

The Lambda hosted API is a default one created by `Lambda ASP.NET Core Web API` [.NET Core project templates](https://github.com/aws/aws-lambda-dotnet/tree/master#amazonlambdaaspnetcoreserver). To enhance the API with AWS Lambda Powertools, we didn't need to change the Kestrel start up code nor the logic of an API. We added standard logging commands, metrics emission, and tracing instrumentation code. 

### Logging
Logging uses the context data available in an AWS Lambda Event. This is the reason why to have information about function name, its memory size etc `[Logging]` attribute needs to be placed on a method with the proper input parameters. The handler function seams like a good choice. [`LambdaEntryPoint`](src/LambdaPowertoolsAPI/LambdaEntryPoint.cs) overrides the handler method (`FunctionHandlerAsync`) from the abstract class `Amazon.Lambda.AspNetCoreServer.FunctionHandlerAsync`. The method is pointed as the handler in the function definition in the SAM template.

```yaml
  AspNetCoreFunction:
    Type: AWS::Serverless::Function
    Properties:
      Handler: LambdaPowertoolsAPI::LambdaPowertoolsAPI.LambdaEntryPoint::FunctionHandlerAsync
```

The template defines a log level and service names through environment variables in the [SAM Template](src/LambdaPowertoolsAPI/serverless.template)

```yaml
POWERTOOLS_SERVICE_NAME: aws-lambda-powertools-web-api-sample
POWERTOOLS_LOG_LEVEL: Debug
```

In this example we point to the correlation ID and instruct PowerTools to log an incoming event.

```c#
[Logging(CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest, LogEvent = true)] // we are enabling logging, it needs to be added on method which have Lambda event
```

Later on in the `Get` method of the [`ValuesController`](src/LambdaPowertoolsAPI/Controllers/ValuesController.cs) logs as it would normally do using a `Logger`. The log output contains all additional information injected by the AWS Lambda Powertools:

```json
{"cold_start":true,"correlation_id":"4749a5a8-93ea-464e-8778-3bffc5f9a35d","function_name":"AspNetCoreFunction","function_version":"$LATEST","function_memory_size":256,"function_arn":"arn:aws:lambda:us-east-1:012345678912:function:AspNetCoreFunction","function_request_id":"150991e0-20ec-4776-acd8-0f9290f1e968","timestamp":"2023-06-05T10:18:02.2434792Z","level":"Information","service":"aws-lambda-powertools-web-api-sample","name":"AWS.Lambda.Powertools.Logging.Logger","message":"Log entry information only about getting values? Or maybe something more "}
```

### Metrics

Metrics does not use any context data in the background. The Metrics initialization can occur on any method. The good practice would be to define [Namespace, service and some additional default dimensions](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/cloudwatch_concepts.html) which will be used for every Metric emit call in the single place. The entry point, once again, sounds as the best choice.(_defaultDimensions);`. 
```c#
    // We are defining some default dimensions.
    private Dictionary<string, string> _defaultDimensions = new Dictionary<string, string>{
        {"Environment", Environment.GetEnvironmentVariable("ENVIRONMENT") ??  "Unknown"},
        {"Runtime",Environment.Version.ToString()}
    };

    [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
    [Logging(CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest, LogEvent = true)] // we are enabling logging, it needs to be added on method which have Lambda event
    [Tracing] // Adding a tracing attribute here we will see additional function call which might be important in terms of debugging
    [Metrics] // Metrics need to be initialized. The best place is the entry point opposite on adding attributes on each controller.
    public override Task<APIGatewayProxyResponse> FunctionHandlerAsync(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
    {
        _defaultDimensions.Add("Version", lambdaContext.FunctionVersion);
        // Setting the default dimensions. They will be added to every emitted metric.
        Metrics.SetDefaultDimensions(_defaultDimensions);

```
The `_defaultDimensions` is a variable which will store the default dimensions. They are set as the defaults using`Metrics.SetDefaultDimensions

In this example the Namespace and service are set in the [SAM template](src/LambdaPowertoolsAPI/serverless.template) and they will be added to each Metric automatically. Thus when a resource `GetById` will be called and a `SuccessfulRetrieval` metric will be emitted it will have all default dimensions, namespace and service. It can be observed in the logs or via AWS Console.

```log
2023/05/27/[$LATEST]7e90626ab88447328171eef7c4c3286f 2023-05-27T20:46:42.883000 2023-05-27T20:46:42.883Z        6408f2f1-ecdc-4f02-9480-1da3ed0a0936  info    {"_aws":{"Timestamp":1685220402624,"CloudWatchMetrics":[{"Namespace":"AWSLambdaPowertools","Metrics":[{"Name":"SuccessfulRetrieval","Unit":"Count"}],"Dimensions":[["Service"],["Environment"],["Runtime"],["Version"]]}]},"Service":"aws-lambda-powertools-web-api-sample","Environment":"Unknown","Runtime":"6.0.15","Version":"$LATEST","SuccessfulRetrieval":1}
```

### Tracing

Tracing is enabled by setting the `Tracing` property in the [SAM Template](src/LambdaPowertoolsAPI/serverless.template) to `true`. To see more insides in what are execution times of each endpoint `GET /api/values` and `GET /api/values/{id}` are annotated with the `[TracingAttribute]`. The segments has been named to different them in traces. On the `Get` method, `SegmentName` has been set using property of `Tracing` attribute.

```c#
    [Tracing(SegmentName = "Values::GetById")]
    public string Get(int id)
```

When analyzing a trace for execution of a resource `GetById` it can be observed `Values::GetById` which is equal to `SegmentName`. With the segment name definition API does not need to be verified to understand which `method` was executed 

```log
XRay Event at (2023-05-27T22:46:39.922000) with id (1-64726c2f-3244a3317c5e1f4f738e532c) and duration (3.160s)
 - 3.103s - aws-lambda-powertools-web-api-s-AspNetCoreFunction-ottogomkubjL [HTTP: 200]
 - 2.517s - aws-lambda-powertools-web-api-s-AspNetCoreFunction-ottogomkubjL
   - 0.489s - Initialization
   - 2.459s - Invocation
     - 0.961s - ## FunctionHandlerAsync
       - 0.038s - Values::GetById
   - 0.057s - Overhead`
```

## Use the AWS SAM CLI to build and test locally

Build your application with the `sam build` command.

```bash
sam build -t ./serverless.template
```

The AWS SAM CLI builds a docker image from a Dockerfile and then installs dependencies defined in `src/LambdaPowertoolsAPI.csproj` inside the docker image. The processed template file is saved in the `.aws-sam/build` folder.

Test a single function by invoking it directly with a test event. An event is a JSON document that represents the input that the function receives from the event source. Test events are included in the `events` folder in this project.

Run functions locally and invoke them with the `sam local invoke` command.

```bash
sam local invoke -e ../../events/event.json 
```

The AWS SAM CLI can also emulate your application's API. Use the `sam local start-api` to run the API locally on port 3000.

```bash
sam local start-api
curl http://localhost:3000/api/values
```
The AWS SAM CLI reads the application template to determine the API's routes and the functions that they invoke. The `Events` property on each function's definition includes the route and method for each path.

```yaml
      Events:
        ProxyResource:
          Type: Api
          Properties:
            Path: "/{proxy+}"
            Method: ANY
        RootResource:
          Type: Api
          Properties:
            Path: "/"
            Method: ANY
```


## Fetch, tail, and filter Lambda function logs

To simplify troubleshooting, AWS SAM CLI has a command `sam logs`. `sam logs` lets you fetch logs generated by your deployed Lambda function from the command line. In addition to printing the logs on the terminal, this command has several nifty features to help you quickly find the bug.

`NOTE`: This command works for all AWS Lambda functions; not just the ones you deploy using SAM.

```bash
sam logs -n AspNetCoreFunction --stack-name aws-lambda-powertools-web-api-sample --tail
```


## Lambda function tracing

To simplify troubleshooting, AWS SAM CLI has a command `sam traces`. `sam traces` lets you fetch traces generated by your deployed Api Gateway and Lambda and Api function from the command line. To see the trace for the given   In addition to printing the logs on the terminal, this command has several nifty features to help you quickly find the bug.

`NOTE`: This command works for all AWS Lambda functions; not just the ones you deploy using SAM.

```bash
sam traces --trace-id <x-amzn-trace-id>
```

where `<x-amzn-trace-id>` should be replaced with value of a `x-amzn-trace-id` response header. To see response headers you should pass `-i` to the `curl` command.

## Add a resource to your application

The application template uses AWS Serverless Application Model (AWS SAM) to define application resources. AWS SAM is an extension of AWS CloudFormation with a simpler syntax for configuring common serverless application resources such as functions, triggers, and APIs. For resources not included in [the AWS SAM specification](https://github.com/awslabs/serverless-application-model/blob/master/versions/2016-10-31.md), you can use standard [AWS CloudFormation](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-template-resource-type-ref.html) resource types.


## Cleanup

To delete the sample application that you created, use the AWS SAM CLI. Assuming you used your project name for the stack name, you can run the following:

```bash
sam delete
```

## Resources

See the [AWS SAM developer guide](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html) for an introduction to SAM specification, the AWS SAM CLI, and serverless application concepts.

Next, you can use AWS Serverless Application Repository to deploy ready to use Apps that go beyond hello world samples and learn how authors developed their applications: [AWS Serverless Application Repository main page](https://aws.amazon.com/serverless/serverlessrepo/)


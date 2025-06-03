# Powertools for AWS Lambda .NET - Bedrock Agent Function example

This starter project consists of:
* Function.cs - file contain C# top level statements that define the function to be called for each event and starts the Lambda runtime client.
* AirportService.cs - Static list of airport codes and names used by the function.
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS

## Executable Assembly

.NET Lambda projects that use C# top level statements like this project must be deployed as an executable assembly instead of a class library. To indicate to Lambda that the .NET function is an executable assembly the 
Lambda function handler value is set to the .NET Assembly name. This is different then deploying as a class library where the function handler string includes the assembly, type and method name.

To deploy as an executable assembly the Lambda runtime client must be started to listen for incoming events to process. To start
the Lambda runtime client add the `Amazon.Lambda.RuntimeSupport` NuGet package and add the following code at the end of the
of the file containing top-level statements to start the runtime.

```csharp
await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
        .Build()
        .RunAsync();
```

Pass into the Lambda runtime client a function handler as either an `Action<>` or `Func<>` for the code that 
should be called for each event. If the handler takes in an input event besides `System.IO.Stream` then
the JSON serializer must also be passed into the `Create` method.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Deploy function to AWS Lambda
```
    cd "BedrockAgentFunction/src"
    dotnet lambda package --output-package ../release/BedrockAgentFunction.zip
    cd ../infra
    npm run cdk deploy -- --require-approval never
```
# AWS Powertools for AWS Lambda .NET - Kafka Protobuf Example

This project demonstrates how to use AWS Lambda Powertools for .NET with Amazon MSK (Managed Streaming for Kafka) to process events from Kafka topics.

## Overview

This example showcases a Lambda functions that consume messages from Kafka topics with Protocol Buffers serialization format.

It uses the `AWS.Lambda.Powertools.Kafka.Protobuf` NuGet package to easily deserialize and process Kafka records.

## Project Structure

```bash
examples/Kafka/Protobuf/src/
├── Function.cs         # Entry point for the Lambda function
├── aws-lambda-tools-defaults.json # Default argument settings for AWS Lambda deployment
├── template.yaml       # AWS SAM template for deploying the function
├── CustomerProfile.proto # Protocol Buffers definition file for the data structure used in the Kafka messages
└── kafka-protobuf-event.json # Sample Protocol Buffers event to test the function
```

## Prerequisites

- [Dotnet](https://dotnet.microsoft.com/en-us/download/dotnet) (dotnet10 or later)
- [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html)
- [AWS CLI](https://aws.amazon.com/cli/)
- An AWS account with appropriate permissions
- [Amazon MSK](https://aws.amazon.com/msk/) cluster set up with a topic to consume messages from
- [AWS.Lambda.Powertools.Kafka.Protobuf](https://www.nuget.org/packages/AWS.Lambda.Powertools.Kafka.Protobuf/) NuGet package installed in your project

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/aws-powertools/powertools-lambda-dotnet.git
   ```

2. Navigate to the project directory:

   ```bash
   cd powertools-lambda-dotnet/examples/Kafka/Protobuf/src
   ```

3. Build the project:

   ```bash
   dotnet build
   ```

## Deployment

Deploy the application using the AWS SAM CLI:

```bash
sam build
sam deploy --guided
```

Follow the prompts to configure your deployment.

## Protocol Buffers Format

The Protobuf example handles messages serialized with Protocol Buffers. The schema is defined in a `.proto` file (which would need to be created), and the C# code is generated from that schema.

This requires the `Grpc.Tools` package to deserialize the messages correctly.

And update the `.csproj` file to include the `.proto` files.

```xml
<Protobuf Include="CustomerProfile.proto">
   <GrpcServices>Client</GrpcServices>
   <Access>Public</Access>
   <ProtoCompile>True</ProtoCompile>
   <CompileOutputs>True</CompileOutputs>
   <OutputDir>obj\Debug/net10.0/</OutputDir>
   <Generator>MSBuild:Compile</Generator>
   <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Protobuf>
```

## Usage Examples

Once deployed, you can test the Lambda function by sending a sample Protocol Buffers event to the configured Kafka topic.
You can use the `kafka-protobuf-event.json` file as a sample event to test the function.

### Testing

You can test the function locally using the AWS SAM CLI (Requires Docker to be installed):

```bash
sam local invoke ProtobufDeserializationFunction --event kafka-protobuf-event.json
```

This command simulates an invocation of the Lambda function with the provided event data.

## How It Works

1. **Event Source**: Configure your Lambda functions with an MSK or self-managed Kafka cluster as an event source.
2. **Deserializing Records**: Powertools handles deserializing the records based on the specified format.
3. **Processing**: Each record is processed within the handler function.

## Event Deserialization

Pass the `PowertoolsKafkaProtobufSerializer` to the `[assembly: LambdaSerializer(typeof(PowertoolsKafkaProtobufSerializer))]`:

```csharp
[assembly: LambdaSerializer(typeof(PowertoolsKafkaProtobufSerializer))]
 ```

## Configuration

The SAM template (`template.yaml`) defines three Lambda function:

- **ProtobufDeserializationFunction**: Handles Protobuf-formatted Kafka messages

## Customization

To customize the examples:

1. Modify the schema definitions to match your data structures
2. Update the handler logic to process the records according to your requirements
3. Ensure you have the proper `.proto` files and that they are included in your project for Protocol Buffers serialization/deserialization.

## Resources

- [AWS Lambda Powertools for .NET Documentation](https://docs.aws.amazon.com/powertools/dotnet/)
- [Amazon MSK Documentation](https://docs.aws.amazon.com/msk/)
- [AWS Lambda Developer Guide](https://docs.aws.amazon.com/lambda/)
- [Protocol Buffers Documentation](https://developers.google.com/protocol-buffers)
# AWS Powertools for AWS Lambda .NET - Kafka Avro Example

This project demonstrates how to use AWS Lambda Powertools for .NET with Amazon MSK (Managed Streaming for Kafka) to process events from Kafka topics.

## Overview

This example showcases a Lambda functions that consume messages from Kafka topics with Avro serialization format.

It uses the `AWS.Lambda.Powertools.Kafka.Avro` NuGet package to easily deserialize and process Kafka records.

## Project Structure

```bash
examples/Kafka/Avro/src/
├── Function.cs         # Entry point for the Lambda function
├── aws-lambda-tools-defaults.json # Default argument settings for AWS Lambda deployment
├── template.yaml       # AWS SAM template for deploying the function
├── CustomerProfile.avsc # Avro schema definition file for the data structure used in the Kafka messages
└── kafka-avro-event.json # Sample Avro event to test the function
```

## Prerequisites

- [Dotnet](https://dotnet.microsoft.com/en-us/download/dotnet) (dotnet8 or later)
- [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html)
- [AWS CLI](https://aws.amazon.com/cli/)
- An AWS account with appropriate permissions
- [Amazon MSK](https://aws.amazon.com/msk/) cluster set up with a topic to consume messages from
- [AWS.Lambda.Powertools.Kafka.Avro](https://www.nuget.org/packages/AWS.Lambda.Powertools.Kafka.Avro/) NuGet package installed in your project
- [Avro Tools](https://www.nuget.org/packages/Apache.Avro.Tools/) codegen tool to generate C# classes from the Avro schema

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/aws-powertools/powertools-lambda-dotnet.git
   ```

2. Navigate to the project directory:

   ```bash
   cd powertools-lambda-dotnet/examples/Kafka/Avro/src
   ```

3. Build the project:

   ```bash
   dotnet build
   ```
4. Install the Avro Tools globally to generate C# classes from the Avro schema:

   ```bash
    dotnet tool install --global Apache.Avro.Tools
    ```

## Deployment

Deploy the application using the AWS SAM CLI:

```bash
sam build
sam deploy --guided
```

Follow the prompts to configure your deployment.

## Avro Format
Avro is a binary serialization format that provides a compact and efficient way to serialize structured data. It uses schemas to define the structure of the data, which allows for robust data evolution.

In this example we provide a schema called `CustomerProfile.avsc`. The schema is used to serialize and deserialize the data in the Kafka messages.

The classes are generated from the .cs file using the Avro Tools command:

```xml
 <Target Name="GenerateAvroClasses" BeforeTargets="CoreCompile">
     <Exec Command="avrogen -s $(ProjectDir)CustomerProfile.avsc $(ProjectDir)Generated"/>
 </Target>
```

## Usage Examples

Once deployed, you can test the Lambda function by sending a sample Avro event to the configured Kafka topic.
You can use the `kafka-avro-event.json` file as a sample event to test the function.

### Testing

You can test the function locally using the AWS SAM CLI (Requires Docker to be installed):

```bash
sam local invoke AvroDeserializationFunction --event kafka-avro-event.json
```

This command simulates an invocation of the Lambda function with the provided event data.

## How It Works

1. **Event Source**: Configure your Lambda functions with an MSK or self-managed Kafka cluster as an event source.
2. **Deserializing Records**: Powertools handles deserializing the records based on the specified format.
3. **Processing**: Each record is processed within the handler function.

## Event Deserialization

Pass the `PowertoolsKafkaAvroSerializer` to the `LambdaBootstrapBuilder.Create()` method to enable Avro deserialization of Kafka records:

```csharp
await LambdaBootstrapBuilder.Create((Func<ConsumerRecords<string, CustomerProfile>, ILambdaContext, string>?)Handler,
        new PowertoolsKafkaAvroSerializer()) // Use PowertoolsKafkaAvroSerializer for Avro serialization
    .Build()
    .RunAsync();
 ```

## Configuration

The SAM template (`template.yaml`) defines three Lambda function:

- **AvroDeserializationFunction**: Handles Avro-formatted Kafka messages

## Customization

To customize the examples:

1. Modify the schema definitions to match your data structures
2. Update the handler logic to process the records according to your requirements

## Resources

- [AWS Lambda Powertools for .NET Documentation](https://docs.powertools.aws.dev/lambda/dotnet/)
- [Amazon MSK Documentation](https://docs.aws.amazon.com/msk/)
- [AWS Lambda Developer Guide](https://docs.aws.amazon.com/lambda/)
- [Apache Avro Documentation](https://avro.apache.org/docs/)
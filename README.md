# Powertools for AWS Lambda (.NET)

![aws provider](https://img.shields.io/badge/provider-AWS-orange?logo=amazon-aws&color=ff9900)
[![Build](https://github.com/aws-powertools/powertools-lambda-dotnet/actions/workflows/build.yml/badge.svg?branch=develop)](https://github.com/aws-powertools/powertools-lambda-dotnet/actions/workflows/build.yml)
[![codecov.io](https://codecov.io/github/aws-powertools/powertools-lambda-dotnet/branch/develop/graphs/badge.svg)](https://app.codecov.io/gh/aws-powertools/powertools-lambda-dotnet)
[![dotnet support](https://img.shields.io/badge/dotnet-.NET_8.0_%7C_10.0-blue?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AWS.Lambda.Powertools.Logging.svg)](https://www.nuget.org/packages?q=AWS.Lambda.Powertools) [![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/aws-powertools/powertools-lambda-dotnet/badge)](https://scorecard.dev/viewer/?uri=github.com/aws-powertools/powertools-lambda-dotnet)
[![Discord](https://img.shields.io/badge/Discord-Join_Community-7289da.svg)](https://discord.gg/B8zZKbbyET)

Powertools for AWS Lambda (.NET) is a developer toolkit to implement Serverless [best practices and increase developer velocity](https://docs.aws.amazon.com/powertools/dotnet/#features).

> Also available in [Python](https://github.com/aws-powertools/powertools-lambda-python), [TypeScript](https://github.com/aws-powertools/powertools-lambda-typescript), and [Java](https://github.com/aws-powertools/powertools-lambda-java).

**[📜 Documentation](https://docs.aws.amazon.com/powertools/dotnet/)** | **[NuGet](https://www.nuget.org/packages?q=AWS.Lambda.Powertools)** | **[Roadmap](https://github.com/orgs/aws-powertools/projects/6)** | **[Examples](#examples)**

## Features

Core utilities:

* **[Logging](https://docs.aws.amazon.com/powertools/dotnet/core/logging/)** - structured JSON logging with Lambda context, cold start, correlation IDs, custom keys and optional event logging. Works with `ILogger`, ASP.NET Core and Native AOT.

* **[Metrics](https://docs.aws.amazon.com/powertools/dotnet/core/metrics/)** - custom metrics written asynchronously with [CloudWatch Embedded Metric Format (EMF)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format.html),
  with no synchronous calls to CloudWatch. `AWS.Lambda.Powertools.Metrics.AspNetCore` adds middleware and endpoint filters for ASP.NET Core.

* **[Tracing](https://docs.aws.amazon.com/powertools/dotnet/core/tracing/)** - AWS X-Ray tracing for your handler, AWS SDK calls and outbound HTTP requests, with a `ColdStart` annotation added for you and support for custom annotations and metadata.

Event handlers:

* **[AppSync Events](https://docs.aws.amazon.com/powertools/dotnet/core/event_handler/appsync_events/)** - route AWS AppSync Events publish and subscribe operations to handlers by channel path, with support for wildcards and aggregated processing.

* **[Bedrock Agent Functions](https://docs.aws.amazon.com/powertools/dotnet/core/event_handler/bedrock_agent_function/)** - register tools for Amazon Bedrock Agents and let the resolver handle parameter binding and response formatting. Also available for ASP.NET Core.

Utilities:

* **[Parameters](https://docs.aws.amazon.com/powertools/dotnet/utilities/parameters/)** - retrieve and cache values from [AWS Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html), [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/), [AWS AppConfig](https://docs.aws.amazon.com/appconfig/latest/userguide/what-is-appconfig.html)
  or [Amazon DynamoDB](https://aws.amazon.com/dynamodb/), or bring your own provider.

* **[Idempotency](https://docs.aws.amazon.com/powertools/dotnet/utilities/idempotency/)** - make Lambda functions safe to retry by storing results in Amazon DynamoDB and returning them for repeated events.

* **[Batch Processing](https://docs.aws.amazon.com/powertools/dotnet/utilities/batch-processing/)** - handle partial failures when processing batches from Amazon SQS, Amazon Kinesis Data Streams and Amazon DynamoDB Streams.

* **[Kafka Consumer](https://docs.aws.amazon.com/powertools/dotnet/utilities/kafka/)** - deserialize Avro, Protobuf and JSON keys and values from Amazon MSK and self-managed Apache Kafka events, with Schema Registry support.

* **[JMESPath Functions](https://docs.aws.amazon.com/powertools/dotnet/utilities/jmespath-functions/)** - extract data from event payloads with JMESPath, including built-in functions to decode JSON strings, base64 and compressed data.

* **[Lambda Metadata](https://docs.aws.amazon.com/powertools/dotnet/utilities/metadata/)** - read execution environment metadata, such as the Availability Zone ID, from the Lambda Metadata Endpoint, cached for the lifetime of the execution environment.

### Installation

The utilities are published as separate NuGet packages, so you only install what you use. Browse them on the [NuGet Gallery](https://www.nuget.org/packages?q=AWS.Lambda.Powertools) or install with `dotnet add package`:

| Utility | Package |
| --- | --- |
| Logging | [`AWS.Lambda.Powertools.Logging`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Logging) |
| Metrics | [`AWS.Lambda.Powertools.Metrics`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Metrics) |
| Metrics for ASP.NET Core | [`AWS.Lambda.Powertools.Metrics.AspNetCore`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Metrics.AspNetCore) |
| Tracing | [`AWS.Lambda.Powertools.Tracing`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Tracing) |
| AppSync Events | [`AWS.Lambda.Powertools.EventHandler`](https://www.nuget.org/packages/AWS.Lambda.Powertools.EventHandler) |
| Bedrock Agent Functions | [`AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction`](https://www.nuget.org/packages/AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction) |
| Bedrock Agent Functions for ASP.NET Core | [`AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore`](https://www.nuget.org/packages/AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.AspNetCore) |
| Parameters | [`AWS.Lambda.Powertools.Parameters`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Parameters) |
| Idempotency | [`AWS.Lambda.Powertools.Idempotency`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Idempotency) |
| Batch Processing | [`AWS.Lambda.Powertools.BatchProcessing`](https://www.nuget.org/packages/AWS.Lambda.Powertools.BatchProcessing) |
| Kafka Consumer (Avro) | [`AWS.Lambda.Powertools.Kafka.Avro`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Kafka.Avro) |
| Kafka Consumer (Protobuf) | [`AWS.Lambda.Powertools.Kafka.Protobuf`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Kafka.Protobuf) |
| Kafka Consumer (JSON) | [`AWS.Lambda.Powertools.Kafka.Json`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Kafka.Json) |
| JMESPath Functions | [`AWS.Lambda.Powertools.JMESPath`](https://www.nuget.org/packages/AWS.Lambda.Powertools.JMESPath) |
| Lambda Metadata | [`AWS.Lambda.Powertools.Metadata`](https://www.nuget.org/packages/AWS.Lambda.Powertools.Metadata) |

For example:

```bash
dotnet add package AWS.Lambda.Powertools.Logging
```

## Examples

Each utility's [documentation](https://docs.aws.amazon.com/powertools/dotnet/) includes code examples. They're kept in [docs/snippets](docs/snippets/) and compiled against the libraries on every change.

## Other members of the Powertools for AWS Lambda family

Not using .NET? No problem, we have you covered. Here are the other members of the Powertools for AWS Lambda family:

* [Powertools for AWS Lambda (Python)](https://github.com/aws-powertools/powertools-lambda-python)
* [Powertools for AWS Lambda (Java)](https://github.com/aws-powertools/powertools-lambda-java)
* [Powertools for AWS Lambda (TypeScript)](https://github.com/aws-powertools/powertools-lambda-typescript)

## How to support Powertools for AWS Lambda (.NET)?

### Becoming a reference customer

Knowing which companies are using this library is important to help prioritize the project internally. If your company is using Powertools for AWS Lambda (.NET), you can request to have your name and logo added to the README file by raising a [Support Powertools for AWS Lambda (.NET) (become a reference)](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/new?assignees=&labels=customer-reference&template=support_powertools.yml&title=%5BSupport+Lambda+Powertools%5D%3A+%3Cyour+organization+name%3E) issue.

The following companies, among others, use Powertools:

* [Caylent](https://caylent.com/)
* [Instil Software](https://instil.co/)
* [Pushpay](https://pushpay.com/)

### Sharing your work

Share what you built with Powertools for AWS Lambda (.NET): blog posts, workshops, talks, sample apps and more. See what the community has already shared on the [We Made This](https://docs.aws.amazon.com/powertools/dotnet/we_made_this/) page.

### 👋 Contributing

We welcome contributions from developers of all levels. Read the [contributing guidelines](CONTRIBUTING.md) to get started, and look for issues labelled [`good first issue`](https://github.com/aws-powertools/powertools-lambda-dotnet/labels/good%20first%20issue) or [`help wanted`](https://github.com/aws-powertools/powertools-lambda-dotnet/labels/help%20wanted).

[![Star History Chart](https://api.star-history.com/svg?repos=aws-powertools/powertools-lambda-dotnet&type=Date)](https://star-history.com/#aws-powertools/powertools-lambda-dotnet&Date)

## Credits

* Structured logging initial implementation from [aws-lambda-logging](https://gitlab.com/hadrien/aws_lambda_logging)
* Powertools for AWS Lambda (.NET) idea [DAZN Powertools](https://github.com/getndazn/dazn-lambda-powertools/)

## Connect

* **Powertools for AWS Lambda on Discord**: `#dotnet` - **[Invite link](https://discord.gg/B8zZKbbyET)**
* **Email**: <aws-lambda-powertools-feedback@amazon.com>

## Security disclosures

If you think you've found a potential security issue, please do not post it in the Issues. Instead, please follow the [AWS vulnerability reporting instructions](https://aws.amazon.com/security/vulnerability-reporting/) or [email AWS security directly](mailto:aws-security@amazon.com).

## License

This project is licensed under the [Apache-2.0 License](LICENSE).

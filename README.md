# Powertools for AWS Lambda (.NET)

![aws provider](https://img.shields.io/badge/provider-AWS-orange?logo=amazon-aws&color=ff9900)
[![Build](https://github.com/aws-powertools/powertools-lambda-dotnet/actions/workflows/build.yml/badge.svg?branch=develop)](https://github.com/aws-powertools/powertools-lambda-dotnet/actions/workflows/build.yml)
[![Join our Discord](https://dcbadge.vercel.app/api/server/B8zZKbbyET)](https://discord.gg/B8zZKbbyET)

Powertools is a developer toolkit to implement Serverless [best practices and increase developer velocity](https://docs.powertools.aws.dev/lambda-dotnet/#features).

**[📜 Documentation](https://docs.powertools.aws.dev/lambda-dotnet/)** | **[NuGet](https://www.nuget.org/)** | **[Roadmap](https://github.com/aws-powertools/powertools-lambda-roadmap/projects/1)** | **[Examples](#examples)**

> **Join us on the AWS Developers Slack at `#lambda-powertools`** - **[Invite, if you don't have an account](https://join.slack.com/t/awsdevelopers/shared_invite/zt-gu30gquv-EhwIYq3kHhhysaZ2aIX7ew)**

## Features

Powertools for AWS Lambda (.NET) provides three core utilities:

* **[Logging](https://docs.powertools.aws.dev/lambda-dotnet/core/logging/)** - provides a custom logger class that outputs structured JSON. It allows you to pass in strings or more complex objects, and will take care of serializing the log output. Common use cases—such as logging the Lambda event payload and capturing cold start information—are handled for you, including appending custom keys to the logger at anytime.

* **[Metrics](https://docs.powertools.aws.dev/lambda-dotnet/core/metrics/)** - makes collecting custom metrics from your application simple, without the need to make synchronous requests to external systems. This functionality is powered by [Amazon CloudWatch Embedded Metric Format (EMF)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format.html), which allows for capturing metrics asynchronously.

* **[Tracing](https://docs.powertools.aws.dev/lambda-dotnet/core/tracing/)** - provides a simple way to send traces from functions to AWS X-Ray to provide visibility into function calls, interactions with other AWS services, or external HTTP requests. Annotations can easily be added to traces to allow filtering traces based on key information. For example, when using Tracer, a ColdStart annotation is created for you so you can easily group and analyze traces where there was an initialization overhead.

* **[Parameters (developer preview)](https://docs.powertools.aws.dev/lambda-dotnet/utilities/parameters/)** - provides high-level functionality to retrieve one or multiple parameter values from [AWS Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html), [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/), or [Amazon DynamoDB](https://aws.amazon.com/dynamodb/). We also provide extensibility to bring your own providers.

* **[Idempotency (developer preview)](https://docs.powertools.aws.dev/lambda-dotnet/utilities/idempotency/)** - The idempotency utility provides a simple solution to convert your Lambda functions into idempotent operations which are safe to retry.

### Installation

The Powertools for AWS Lambda (.NET) utilities (.NET 6) are available as NuGet packages. You can install the packages from [NuGet Gallery](https://www.nuget.org/packages?q=AWS+Lambda+Powertools*) or from Visual Studio editor by searching `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Logging](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Logging):

    `dotnet add package AWS.Lambda.Powertools.Logging`

* [AWS.Lambda.Powertools.Metrics](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Metrics):

    `dotnet add package AWS.Lambda.Powertools.Metrics`

* [AWS.Lambda.Powertools.Tracing](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Tracing):

    `dotnet add package AWS.Lambda.Powertools.Tracing`

* [AWS.Lambda.Powertools.Parameters](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Parameters):

    `dotnet add package AWS.Lambda.Powertools.Parameters`

* [AWS.Lambda.Powertools.Idempotency](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Idempotency):

    `dotnet add package AWS.Lambda.Powertools.Idempotency`

## Examples

We have provided examples focused specifically on each of the utilities. Each solution comes with an AWS Serverless Application Model (AWS SAM) templates to run your functions as a Zip package using the AWS Lambda .NET 6 managed runtime; or as a container package using the AWS base images for .NET.

* **[Logging example](examples/Logging/)**
* **[Metrics example](examples/Metrics/)**
* **[Tracing example](examples/Tracing/)**
* **[Serverless API example](examples/ServerlessApi/)**

* **[Parameters example](examples/Parameters/)**
* **[Idempotency example](examples/Idempotency)**

## Other members of the Powertools for AWS Lambda family

Not using .NET? No problem, we have you covered. Here are the other members of the Powertools for AWS Lambda family:

* [Powertools for AWS Lambda (Python)](https://github.com/aws-powertools/powertools-lambda-python)
* [Powertools for AWS Lambda (Java)](https://github.com/aws-powertools/powertools-lambda-java)
* [Powertools for AWS Lambda (TypeScript)](https://github.com/aws-powertools/powertools-lambda-typescript)

## Credits

* Structured logging initial implementation from [aws-lambda-logging](https://gitlab.com/hadrien/aws_lambda_logging)
* Powertools for AWS Lambda (.NET) idea [DAZN Powertools](https://github.com/getndazn/dazn-lambda-powertools/)

## 👋 Contributing

We welcome contributions from developers of all levels to our open-source project on GitHub. If you'd like to contribute, please check our contributing guidelines and help make this project more accessible.

[![Star History Chart](https://api.star-history.com/svg?repos=aws-powertools/powertools-lambda-dotnet&type=Date)](https://star-history.com/#aws-powertools/powertools-lambda-dotnet&Date)

## Connect

* **Powertools for AWS Lambda on Discord**: `#dotnet` - **[Invite link](https://discord.gg/B8zZKbbyET)**
* **Email**: <aws-lambda-powertools-feedback@amazon.com>

## License

This project is licensed under the Apache-2.0 License.

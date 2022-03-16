# AWS Lambda Powertools for .NET

![aws provider](https://img.shields.io/badge/provider-AWS-orange?logo=amazon-aws&color=ff9900) [![Build](https://github.com/awslabs/aws-lambda-powertools-dotnet/actions/workflows/build.yml/badge.svg?branch=develop)](https://github.com/awslabs/aws-lambda-powertools-dotnet/actions/workflows/build.yml)

AWS Lambda Powertools for .NET (C#) is suite of utilities for AWS Lambda functions to simplify implementation of serverless observability best practices such as tracing, structured logging, custom metrics.

**[📜 Documentation](https://awslabs.github.io/aws-lambda-powertools-dotnet/)** | **[NuGet](https://www.nuget.org/)** | **[Roadmap](https://github.com/awslabs/aws-lambda-powertools-roadmap/projects/1)** | **[Examples](#examples)** | **[Blog post](https://aws.amazon.com/blogs/opensource/simplifying-serverless-best-practices-with-lambda-powertools/)**

> **Join us on the AWS Developers Slack at `#lambda-powertools`** - **[Invite, if you don't have an account](https://join.slack.com/t/awsdevelopers/shared_invite/zt-gu30gquv-EhwIYq3kHhhysaZ2aIX7ew)**

## Features

Lambda Powertools provides three core utilities:

* **[Logging](https://awslabs.github.io/aws-lambda-powertools-dotnet/core/logging/)** - provides a custom logger class that outputs structured JSON. It allows you to pass in strings or more complex objects, and will take care of serializing the log output. Common use cases—such as logging the Lambda event payload and capturing cold start information—are handled for you, including appending custom keys to the logger at anytime.

* **[Metrics](https://awslabs.github.io/aws-lambda-powertools-dotnet/core/metrics/)** - makes collecting custom metrics from your application simple, without the need to make synchronous requests to external systems. This functionality is powered by [Amazon CloudWatch Embedded Metric Format (EMF)](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch_Embedded_Metric_Format.html), which allows for capturing metrics asynchronously.

* **[Tracing](https://awslabs.github.io/aws-lambda-powertools-dotnet/core/tracing/)** - provides a simple way to send traces from functions to AWS X-Ray to provide visibility into function calls, interactions with other AWS services, or external HTTP requests. Annotations easily can be added to traces to allow filtering traces based on key information. For example, when using Tracer, a ColdStart annotation is created for you, so you can easily group and analyze traces where there was an initialization overhead.

### Installation

The AWS Lambda Powertools for .NET utilities (.NET 6) are available as NuGet packages. You can install the packages from NuGet gallery or from Visual Studio editor. Search `AWS.Lambda.Powertools*` to see various utilities available.Powertools is available on NuGet.

* [AWS.Lambda.Powertools.Logging](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Logging):

    `dotnet add package AWS.Lambda.Powertools.Logging`

* [AWS.Lambda.Powertools.Metrics](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Metrics):

    `dotnet add package AWS.Lambda.Powertools.Metrics`

* [AWS.Lambda.Powertools.Tracing](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Tracing):

    `dotnet add package AWS.Lambda.Powertools.Tracing`

## Examples

We have provided examples focused specifically on each of the utilities. Each solution comes with AWS Serverless Application Model (AWS SAM) templates to run your functions as Zip package using the AWS Lambda .NET 6 managed runtime, or as a container package using the AWS base images for .NET.

* **[Logging example](examples/Logging/README.md)**
* **[Metrics example](examples/Metrics/README.md)**
* **[Tracing example](examples/Tracing/README.md)**

## Other members of the AWS Lambda Powertools family

Not using .NET? No problem we have you covered. Here are the other members of the AWS Lambda Powertools family:

* [AWS Lambda Powertools for Python](https://github.com/awslabs/aws-lambda-powertools-python)
* [AWS Lambda Powertools for Java](https://github.com/awslabs/aws-lambda-powertools-java)
* [AWS Lambda Powertools for TypeScript](https://github.com/awslabs/aws-lambda-powertools-typescript)

## Credits

* Structured logging initial implementation from [aws-lambda-logging](https://gitlab.com/hadrien/aws_lambda_logging)
* Powertools idea [DAZN Powertools](https://github.com/getndazn/dazn-lambda-powertools/)

## Connect

* **AWS Developers Slack**: `#lambda-powertools` - **[Invite, if you don't have an account](https://join.slack.com/t/awsdevelopers/shared_invite/zt-yryddays-C9fkWrmguDv0h2EEDzCqvw)**
* **Email**: aws-lambda-powertools-feedback@amazon.com

## License

This project is licensed under the Apache-2.0 License.

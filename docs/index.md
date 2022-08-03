---
title: AWS Lambda Powertools for .NET (developer preview)
description: AWS Lambda Powertools for .NET (developer preview)
---

# AWS Lambda Powertools for .NET

AWS Lambda Powertools for .NET (which from here will be referred as Powertools) is a suite of utilities for [AWS Lambda](https://aws.amazon.com/lambda/) functions to ease adopting best practices such as tracing, structured logging, custom metrics, and more. Please note, Powertools is **optimized for .NET 6 only**.

The GitHub repository for this project can be found [here](https://github.com/awslabs/aws-lambda-powertools-dotnet).

!!! warning  "Do not use this library in production"

    **AWS Lambda Powertools for .NET is currently released in preview** and is intended strictly for feedback purposes only. This version is not stable, and significant breaking changes might incur as part of the upcoming production-ready release.

    Your support is much appreciated. If you encounter any problems, [please raise an issue](https://github.com/awslabs/aws-lambda-powertools-dotnet/issues/new/choose).

    **Do not use this library for production workloads.**

## Available Powertools libraries

| Utility | Description
| ------------------------------------------------- | ---------------------------------------------------------------------------------
[Tracing](./core/tracing.md) | Decorators and utilities to trace Lambda function handlers, and both synchronous and asynchronous functions
[Logger](./core/logging.md) | Structured logging made easier, and decorator to enrich structured logging with key Lambda context details
[Metrics](./core/metrics.md) | Custom AWS metrics created asynchronously via CloudWatch Embedded Metric Format (EMF)

## Install

Powertools are available as NuGet packages. You can install the packages from NuGet gallery or from Visual Studio editor. Search `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Tracing](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Tracing):

    `dotnet nuget add AWS.Lambda.Powertools.Tracing`

* [AWS.Lambda.Powertools.Logging](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Logging):

    `dotnet nuget add AWS.Lambda.Powertools.Logging`

* [AWS.Lambda.Powertools.Metrics](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Metrics):

    `dotnet nuget add AWS.Lambda.Powertools.Metrics`

### SAM CLI custom template

We have provided you with a custom template for the Serverless Application Model (AWS SAM) command-line interface (CLI). This generates a starter project that allows you to interactively choose the Powertools features that enables you to include in your project.

```bash
sam init --location https://github.com/aws-samples/cookiecutter-aws-sam-dotnet
```

To use the SAM CLI, you need the following tools.

* SAM CLI - [Install the SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html)
* .NET 6.0 (LTS)  - [Install .NET 6.0](https://www.microsoft.com/net/download)
* Docker - [Install Docker community edition](https://hub.docker.com/search/?type=edition&offering=community)

### Examples

We have provided a few examples that should you how to use the each of the core Powertools features.

* [Tracing](https://github.com/awslabs/aws-lambda-powertools-dotnet/tree/main/examples/Tracing){target="_blank"} example
* [Logging](https://github.com/awslabs/aws-lambda-powertools-dotnet/tree/main/examples/Logging/){target="_blank"} example
* [Metrics](https://github.com/awslabs/aws-lambda-powertools-dotnet/tree/main/examples/Metrics/){target="_blank"} example

## Other members of the AWS Lambda Powertools family

Not using .NET? No problem we have you covered. Here are the other members of the AWS Lambda Powertools family:

* [AWS Lambda Powertools for Python](https://github.com/awslabs/aws-lambda-powertools-python)
* [AWS Lambda Powertools for Java](https://github.com/awslabs/aws-lambda-powertools-java)
* [AWS Lambda Powertools for TypeScript](https://github.com/awslabs/aws-lambda-powertools-typescript)

## Connect

* **AWS Developers Slack**: `#lambda-powertools` - [Invite, if you don't have an account](https://join.slack.com/t/awsdevelopers/shared_invite/zt-yryddays-C9fkWrmguDv0h2EEDzCqvw){target="_blank"}
* **Email**: aws-lambda-powertools-feedback@amazon.com

## Credits

* Credits for the Lambda Powertools idea go to [DAZN](https://github.com/getndazn){target="_blank"} and their [DAZN Lambda Powertools](https://github.com/getndazn/dazn-lambda-powertools/){target="_blank"}.

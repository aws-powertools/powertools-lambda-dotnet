---
title: Powertools for AWS Lambda (.NET)
description: Powertools for AWS Lambda (.NET)
---

<!-- markdownlint-disable MD043 MD013 -->

# Powertools for AWS Lambda (.NET)

Powertools for AWS Lambda (.NET) (which from here will be referred as Powertools) is a suite of utilities for [AWS Lambda](https://aws.amazon.com/lambda/) functions to ease adopting best practices such as tracing, structured logging, custom metrics, and more. Please note, **Powertools for AWS Lambda (.NET) is optimized for .NET 6+**.

???+ tip
    Powertools is also available for [Python](https://awslabs.github.io/aws-lambda-powertools-python/){target="_blank"}, [Java](https://awslabs.github.io/aws-lambda-powertools-java/){target="_blank"}, and [TypeScript](https://awslabs.github.io/aws-lambda-powertools-typescript/latest/){target="_blank"}.

??? hint "Support this project by becoming a reference customer or sharing your work :heart:"

    You can choose to support us in three ways:

    1) [**Become a reference customers**](https://github.com/aws-powertools/lambda-dotnet/issues/new?assignees=&labels=customer-reference&template=support_powertools.yml&title=%5BSupport+Lambda+Powertools%5D%3A+%3Cyour+organization+name%3E). This gives us permission to list your company in our documentation.

    2) [**Share your work**](https://github.com/aws-powertools/lambda-dotnet/issues/new?assignees=&labels=community-content&template=share_your_work.yml&title=%5BI+Made+This%5D%3A+%3CTITLE%3E). Blog posts, video, sample projects you used Powertools!

## Features

Core utilities such as Tracing, Logging, and Metrics will be available across all Powertools for AWS Lambda languages. Additional utilities are subjective to each language ecosystem and customer demand.

| Utility | Description
| ------------------------------------------------- | ---------------------------------------------------------------------------------
[Tracing](./core/tracing.md) | Decorators and utilities to trace Lambda function handlers, and both synchronous and asynchronous functions
[Logger](./core/logging.md) | Structured logging made easier, and decorator to enrich structured logging with key Lambda context details
[Metrics](./core/metrics.md) | Custom AWS metrics created asynchronously via CloudWatch Embedded Metric Format (EMF)

## Install

Powertools for AWS Lambda (.NET) is available as NuGet packages. You can install the packages from NuGet gallery or from Visual Studio editor. Search `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Tracing](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Tracing):

    `dotnet nuget add AWS.Lambda.Powertools.Tracing`

* [AWS.Lambda.Powertools.Logging](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Logging):

    `dotnet nuget add AWS.Lambda.Powertools.Logging`

* [AWS.Lambda.Powertools.Metrics](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Metrics):

    `dotnet nuget add AWS.Lambda.Powertools.Metrics`

### SAM CLI custom template

We have provided you with a custom template for the Serverless Application Model (AWS SAM) command-line interface (CLI). This generates a starter project that allows you to interactively choose the Powertools for AWS Lambda (.NET) features that enables you to include in your project.

```bash
sam init --location https://github.com/aws-samples/cookiecutter-aws-sam-dotnet
```

To use the SAM CLI, you need the following tools.

* SAM CLI - [Install the SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html)
* .NET 6.0 (LTS)  - [Install .NET 6.0](https://www.microsoft.com/net/download)
* Docker - [Install Docker community edition](https://hub.docker.com/search/?type=edition&offering=community)

## Examples

We have provided a few examples that should you how to use the each of the core Powertools for AWS Lambda (.NET) features.

* [Tracing](https://github.com/aws-powertools/lambda-dotnet/tree/main/examples/Tracing){target="_blank"} example
* [Logging](https://github.com/aws-powertools/lambda-dotnet/tree/main/examples/Logging/){target="_blank"} example
* [Metrics](https://github.com/aws-powertools/lambda-dotnet/tree/main/examples/Metrics/){target="_blank"} example

## Connect

* **Powertools for AWS Lambda (.NET) on Discord**: `#dotnet` - **[Invite link](https://discord.gg/B8zZKbbyET){target="_blank"}**
* **Email**: aws-lambda-powertools-feedback@amazon.com

## Tenets

These are our core principles to guide our decision making.

* **AWS Lambda only**. We optimize for AWS Lambda function environments and supported runtimes only. Utilities might work with web frameworks and non-Lambda environments, though they are not officially supported.
* **Eases the adoption of best practices**. The main priority of the utilities is to facilitate best practices adoption, as defined in the AWS Well-Architected Serverless Lens; all other functionality is optional.
* **Keep it lean**. Additional dependencies are carefully considered for security and ease of maintenance, and prevent negatively impacting startup time.
* **We strive for backwards compatibility**. New features and changes should keep backwards compatibility. If a breaking change cannot be avoided, the deprecation and migration process should be clearly defined.
* **We work backwards from the community**. We aim to strike a balance of what would work best for 80% of customers. Emerging practices are considered and discussed via Requests for Comment (RFCs)
* **Idiomatic**. Utilities follow programming language idioms and language-specific best practices.
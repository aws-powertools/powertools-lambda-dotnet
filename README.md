# AWS Lambda Powertools (.NET)

![aws provider](https://img.shields.io/badge/provider-AWS-orange?logo=amazon-aws&color=ff9900) [![Build](https://github.com/awslabs/aws-lambda-powertools-dotnet/actions/workflows/build.yml/badge.svg?branch=develop)](https://github.com/awslabs/aws-lambda-powertools-dotnet/actions/workflows/build.yml)

A suite of .NET (C#) utilities for AWS Lambda functions to ease adopting best practices such as tracing, structured logging, custom metrics, and more. ([AWS Lambda Powertools Python](https://github.com/awslabs/aws-lambda-powertools-python) and [AWS Lambda Powertools Java](https://github.com/awslabs/aws-lambda-powertools-java) are also available).

**[📜 Documentation](https://awslabs.github.io/aws-lambda-powertools-dotnet/)** | **[nuget](https://www.nuget.org/)** | **[Roadmap](https://github.com/awslabs/aws-lambda-powertools-roadmap/projects/1)** | **[Quick hello world example](https://github.com/aws-samples/cookiecutter-aws-sam-dotnet)** | **[Detailed blog post](https://aws.amazon.com/blogs/opensource/simplifying-serverless-best-practices-with-lambda-powertools/)**

> **Join us on the AWS Developers Slack at `#lambda-powertools`** - **[Invite, if you don't have an account](https://join.slack.com/t/awsdevelopers/shared_invite/zt-gu30gquv-EhwIYq3kHhhysaZ2aIX7ew)**

## Features

* **[Tracing](https://awslabs.github.io/aws-lambda-powertools-python/latest/core/tracer/)** - Decorators and utilities to trace Lambda function handlers, and both synchronous and asynchronous functions
* **[Logging](https://awslabs.github.io/aws-lambda-powertools-python/latest/core/logger/)** - Structured logging made easier, and decorator to enrich structured logging with key Lambda context details
* **[Metrics](https://awslabs.github.io/aws-lambda-powertools-python/latest/core/metrics/)** - Custom Metrics created asynchronously via CloudWatch Embedded Metric Format (EMF)

### Installation

With Nuget...

## Examples

* [TODO: Add example project here]

## Credits

* Structured logging initial implementation from [aws-lambda-logging](https://gitlab.com/hadrien/aws_lambda_logging)
* Powertools idea [DAZN Powertools](https://github.com/getndazn/dazn-lambda-powertools/)

## License

This project is licensed under the Apache-2.0 License.

---
title: AWS Lambda Powertools for .NET references
description: AWS Lambda Powertools for .NET references
---

## Environment variables

!!! info
    **Explicit parameters take precedence over environment variables.**

| Environment variable | Description | Utility | Default |
| ------------------------------------------------- | --------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- | ------------------------------------------------- |
| **POWERTOOLS_SERVICE_NAME** | Sets service name used for tracing namespace, metrics dimension and structured logging | All | `"service_undefined"` |
| **POWERTOOLS_LOG_LEVEL** | Sets logging level | [Logging](./core/logger) | `Information` |
| **POWERTOOLS_LOGGER_CASE** | Override the default casing for log keys | [Logging](./core/logging/#configure-log-output-casing) | `SnakeCase` |
| **POWERTOOLS_LOGGER_LOG_EVENT** | Logs incoming event | [Logging](./core/logging) | `false` |
| **POWERTOOLS_LOGGER_SAMPLE_RATE** | Debug log sampling | [Logging](./core/logging) | `0` |
| **POWERTOOLS_METRICS_NAMESPACE** | Sets namespace used for metrics | [Metrics](./core/metrics) | `None` |
| **POWERTOOLS_TRACE_DISABLED** | Disables tracing | [Tracing](./core/tracing) | `false` |
| **POWERTOOLS_TRACER_CAPTURE_RESPONSE** | Captures Lambda or method return as metadata. | [Tracing](./core/tracing) | `true` |
| **POWERTOOLS_TRACER_CAPTURE_ERROR** | Captures Lambda or method exception as metadata. | [Tracing](./core/tracing) | `true` |

## SAM template snippets

### Logging

```yaml
# Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
# SPDX-License-Identifier: MIT-0
AWSTemplateFormatVersion: "2010-09-09"
Transform: AWS::Serverless-2016-10-31
Description: >
  Example project for Powertools Logging utility

# More info about Globals: https://github.com/awslabs/serverless-application-model/blob/master/docs/globals.rst
Globals:
  Function:
    Timeout: 10
    Environment:
      Variables:
        POWERTOOLS_SERVICE_NAME: powertools-dotnet-logging-sample
        POWERTOOLS_LOG_LEVEL: Debug
        POWERTOOLS_LOGGER_LOG_EVENT: true
        POWERTOOLS_LOGGER_CASE: SnakeCase # Allowed values are: CamelCase, PascalCase and SnakeCase

```

### Metrics

```yaml
# Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
# SPDX-License-Identifier: MIT-0
AWSTemplateFormatVersion: "2010-09-09"
Transform: AWS::Serverless-2016-10-31
Description: >
  Example project for Powertools Metrics utility

# More info about Globals: https://github.com/awslabs/serverless-application-model/blob/master/docs/globals.rst
Globals:
  Function:
    Timeout: 10
    Environment:
      Variables:
        POWERTOOLS_SERVICE_NAME: powertools-dotnet-metrics-sample # This can also be set using the Metrics decorator on your handler [Metrics(Namespace = "aws-lambda-powertools"]
        POWERTOOLS_METRICS_NAMESPACE: AWSLambdaPowertools # This can also be set using the Metrics decorator on your handler [Metrics(Namespace = "aws-lambda-powertools"]
```

### Tracing

```yaml
# Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
# SPDX-License-Identifier: MIT-0
AWSTemplateFormatVersion: "2010-09-09"
Transform: AWS::Serverless-2016-10-31
Description: >
  Example project for Powertools tracing utility

# More info about Globals: https://github.com/awslabs/serverless-application-model/blob/master/docs/globals.rst
Globals:
  Function:
    Timeout: 10
    Tracing: Active
    Environment:
      Variables:
        POWERTOOLS_SERVICE_NAME: powertools-dotnet-tracing-sample
        POWERTOOLS_TRACE_DISABLED: true
        POWERTOOLS_TRACER_CAPTURE_RESPONSE: true
        POWERTOOLS_TRACER_CAPTURE_ERROR: true     # To disable tracing (CaptureMode = TracingCaptureMode.Disabled)

```

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
| **POWERTOOLS_METRICS_NAMESPACE** | Sets namespace used for metrics | [Metrics](./core/metrics) | `None` |
| **POWERTOOLS_TRACE_DISABLED** | Explicitly disables tracing | [Tracing](./core/tracer) | `false` |
| **POWERTOOLS_TRACER_CAPTURE_RESPONSE** | Enables/Disables tracing mode to capture method response. | [Tracing](./core/tracer) | `true` |
| **POWERTOOLS_TRACER_CAPTURE_ERROR** | Enables/Disables tracing mode to capture method error. | [Tracing](./core/tracer) | `true` |
| **POWERTOOLS_LOG_LEVEL** | Sets logging level | [Logging](./core/logger) | `Information` |
| **POWERTOOLS_LOGGER_LOG_EVENT** | Logs incoming event | [Logging](./core/logger) | `false` |
| **POWERTOOLS_LOGGER_SAMPLE_RATE** | Debug log sampling | [Logging](./core/logger) | `0` |
| **POWERTOOLS_LOGGER_CASE** | Sets logger output case | [Logging](./core/logger) | `SnakeCase` |

## SAM template snippets

TODO.... 

# Powertools for AWS Lambda (.NET)

A developer toolkit implementing serverless best practices for .NET Lambda functions. Part of the AWS Powertools family (also available in Python, Java, TypeScript).

## Core Utilities

- **Logging** - Structured JSON logging with Lambda context enrichment
- **Metrics** - CloudWatch EMF metrics without synchronous API calls
- **Tracing** - AWS X-Ray integration with annotations and cold start tracking
- **Parameters** - SSM Parameter Store, Secrets Manager, DynamoDB parameter retrieval
- **Idempotency** - Safe retry handling for Lambda functions
- **Batch Processing** - Partial failure handling for SQS, Kinesis, DynamoDB Streams
- **Event Handler** - AppSync Events and Bedrock Agent Function handling
- **Kafka** - Event processing with Avro, JSON, and Protobuf serialization

## Distribution

Published as NuGet packages under `AWS.Lambda.Powertools.*` namespace.

## Target Audience

.NET developers building serverless applications on AWS Lambda who need production-ready observability, resilience, and operational tooling.

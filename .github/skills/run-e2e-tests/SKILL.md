---
name: run-e2e-tests
description: Guide for deploying infrastructure and running E2E tests for Powertools. Use this when asked to run end-to-end tests, deploy test infrastructure, or debug E2E test failures.
---

# Running E2E Tests

This skill guides you through deploying AWS infrastructure and running end-to-end tests for Powertools.

## Prerequisites

Before running E2E tests, ensure you have:
- AWS CLI configured with valid credentials
- AWS CDK installed (`npm install -g aws-cdk`)
- .NET SDK 8.0+ installed
- Sufficient AWS permissions for Lambda, DynamoDB, SQS, Kinesis, CloudWatch

## Directory Structure

```
libraries/tests/e2e/
├── infra/              # CDK stacks for standard tests
├── infra-aot/          # CDK stacks for AOT tests
├── functions/          # Test Lambda functions
│   └── core/           # Core utilities tests
└── InfraShared/        # Shared CDK constructs
```

## Step-by-Step Process

### Step 1: Deploy Standard Infrastructure

```bash
cd libraries/tests/e2e/infra
cdk bootstrap  # Only needed once per account/region
cdk deploy --require-approval never
```

This deploys:
- Test Lambda functions
- DynamoDB tables for Idempotency
- SQS queues for BatchProcessing
- CloudWatch log groups

### Step 2: Deploy AOT Infrastructure (Optional)

For Native AOT tests:

```bash
cd libraries/tests/e2e/infra-aot
cdk deploy CoreStack --require-approval never --context architecture=arm64
```

Options:
- `--context architecture=arm64` - ARM64 architecture (recommended)
- `--context architecture=x86_64` - x86_64 architecture

### Step 3: Run E2E Tests

```bash
# Run all E2E tests
cd libraries
dotnet test --filter "Category=E2E"

# Run specific E2E test project
cd libraries/tests/e2e/functions/core
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~LoggingE2ETests.Should_WriteStructuredLogs"
```

### Step 4: CRITICAL - Destroy Infrastructure

**Always destroy infrastructure after testing to avoid AWS charges:**

```bash
# Destroy standard infrastructure
cd libraries/tests/e2e/infra
cdk destroy --force

# Destroy AOT infrastructure
cd libraries/tests/e2e/infra-aot
cdk destroy --force
```

## Debugging E2E Test Failures

### Check CloudWatch Logs

```bash
# List recent log groups
aws logs describe-log-groups --log-group-name-prefix "/aws/lambda/Powertools"

# Get recent log events
aws logs filter-log-events \
    --log-group-name "/aws/lambda/PowertoolsLoggingTest" \
    --start-time $(date -v-1H +%s000)
```

### Check Lambda Function Status

```bash
aws lambda get-function --function-name PowertoolsLoggingTest
aws lambda invoke --function-name PowertoolsLoggingTest output.json
cat output.json
```

### Common Failures

| Error | Cause | Solution |
|-------|-------|----------|
| `Stack not found` | Infrastructure not deployed | Run `cdk deploy` |
| `Access Denied` | Missing AWS permissions | Check IAM policies |
| `Function timeout` | Cold start or slow operation | Increase timeout in CDK |
| `Resource not found` | Wrong region | Check `AWS_REGION` env var |

## Environment Variables

E2E tests use these environment variables:

```bash
export AWS_REGION=eu-west-1
export POWERTOOLS_SERVICE_NAME=e2e-test
export POWERTOOLS_LOG_LEVEL=Debug
```

## CI/CD Integration

E2E tests are excluded from normal CI runs. They run:
- On-demand via workflow dispatch
- Nightly scheduled runs
- Before releases

To run in CI:
```yaml
- name: Deploy E2E Infrastructure
  run: |
    cd libraries/tests/e2e/infra
    cdk deploy --require-approval never

- name: Run E2E Tests
  run: dotnet test --filter "Category=E2E"

- name: Cleanup Infrastructure
  if: always()
  run: |
    cd libraries/tests/e2e/infra
    cdk destroy --force
```

## Cost Considerations

E2E infrastructure includes:
- Lambda functions (pay per invocation)
- DynamoDB tables (pay per request)
- CloudWatch logs (pay per ingestion)

**Estimated cost**: ~$0.10-0.50 per test run (varies by test duration)

**Always destroy infrastructure after testing!**

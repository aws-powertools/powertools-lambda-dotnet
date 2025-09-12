# E2E Tests Workflow

This document provides instructions on how to set up, deploy, run tests, and destroy the infrastructure for E2E tests.

## Prerequisites

Ensure you have the following tools installed:
- .NET SDK
- AWS CDK
- AWS CLI

## Steps

### 1. Set up your environment

Ensure you have the necessary tools installed, such as .NET SDK, AWS CDK, and AWS CLI.

### 2. Deploy the infrastructure

Navigate to the directory containing your CDK stacks and deploy them:

```sh
cd infra
cdk deploy --require-approval never
cd ../infra-aot
cdk deploy --require-approval never
```
### 3. Run the tests

Navigate to the test project directory and run the tests:

```sh
# example for Core utilities
cd libraries/tests/e2e/functions/core
dotnet test
```

### 4. Destroy

After running the tests, destroy the infrastructure:

```sh
cd infra
cdk destroy --force
cd ../infra-aot
cdk destroy --force
```


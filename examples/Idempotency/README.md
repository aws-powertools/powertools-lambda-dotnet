# Powertools for AWS Lambda (.NET) - Idempotency Example

This project contains source code and supporting files for a serverless application that you can deploy with the AWS Serverless Application Model Command Line Interface (AWS SAM CLI). It includes the following files and folders.

* src - Code for the application's Lambda function.
* events - Invocation events that you can use to invoke the function.
* test - Tests for the application code.
* template.yaml - A template that defines the application's AWS resources.

The application uses several AWS resources, including Lambda function, DynamoDB table and an API Gateway API. These resources are defined in the `template.yaml` file in this project. You can update the template to add AWS resources through the same deployment process that updates your application code.

If you prefer to use an integrated development environment (IDE) to build and test your application, you can use the AWS Toolkit. The AWS Toolkit is an open source plug-in for popular IDEs that uses the AWS SAM CLI to build and deploy serverless applications on AWS. The AWS Toolkit also adds a simplified step-through debugging experience for Lambda function code. See the following links to get started.

* [Visual Studio Code](https://docs.aws.amazon.com/toolkit-for-vscode/latest/userguide/welcome.html)
* [Visual Studio](https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/welcome.html)
* [Rider](https://docs.aws.amazon.com/toolkit-for-jetbrains/latest/userguide/welcome.html)

## Deploy the sample application

The AWS SAM CLI is an extension of the AWS Command Line Interface (AWS CLI) that adds functionality for building and testing Lambda applications. It uses Docker to run your functions in an Amazon Linux environment that matches Lambda. It can also emulate your application's build environment and API.

To use the AWS SAM CLI, you need the following tools.

* AWS SAM CLI - [Install the AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html)
* Docker - [Install Docker community edition](https://hub.docker.com/search/?type=edition&offering=community)

You will need the following for local testing.
* .NET 6.0 - [Install .NET 6.0](https://www.microsoft.com/net/download)

To build and deploy your application for the first time, run the following in your shell. Make sure the `template.yaml` file is in your current directory:

```bash
sam build
sam deploy --guided
```

The first command will build a docker image from a Dockerfile and then copy the source of your application inside the Docker image. The second command will package and deploy your application to AWS, with a series of prompts:

* **Stack Name**: The name of the stack to deploy to CloudFormation. This should be unique to your account and region, and a good starting point would be something matching your project name.
* **AWS Region**: The AWS region you want to deploy your app to.
* **Confirm changes before deploy**: If set to yes, any change sets will be shown to you before execution for manual review. If set to no, the AWS SAM CLI will automatically deploy application changes.
* **Allow SAM CLI IAM role creation**: Many AWS SAM templates, including this example, create AWS IAM roles required for the AWS Lambda function(s) included to access AWS services. By default, these are scoped down to minimum required permissions. To deploy an AWS CloudFormation stack which creates or modifies IAM roles, the `CAPABILITY_IAM` value for `capabilities` must be provided. If permission isn't provided through this prompt, to deploy this example you must explicitly pass `--capabilities CAPABILITY_IAM` to the `sam deploy` command.
* **Save arguments to samconfig.toml**: If set to yes, your choices will be saved to a configuration file inside the project, so that in the future you can just re-run `sam deploy` without parameters to deploy changes to your application.

You can find your API Gateway Endpoint URL in the output values displayed after deployment.

## Invoke Amazon API Gateway

Invoke the Lambda by calling the API Gateway using the following command

```bash
curl -X POST https://[REST-API-ID].execute-api.[REGION].amazonaws.com/Prod/hello/ -H "Content-Type: application/json" -d '{"address": "https://checkip.amazonaws.com"}'
```

## Run tests

Tests use the [Testcontainers for .NET](https://dotnet.testcontainers.org/) which requires a Docker-API compatible container runtime.

Tests are defined in the `test` folder in this project.

```bash
$ dotnet test test/HelloWorld.Test
```

## Cleanup

To delete the sample application that you created, use the AWS SAM CLI. Assuming you used your project name for the stack name, you can run the following:

```bash
$ sam delete
```
# AWS.Lambda.Powertools.Parameters

The Parameters utility provides high-level functionality to retrieve one or multiple parameter values from [AWS Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html), [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/), or [Amazon DynamoDB](https://aws.amazon.com/dynamodb/). Or bring your own providers.

## Key features

* Retrieve one or multiple parameters from the underlying provider
* Cache parameter values for a given amount of time (defaults to 5 seconds)
* Transform parameter values from JSON or base 64 encoded strings
* Bring your own parameter store provider

## Read the docs

For a full list of features go to [docs.powertools.aws.dev/lambda/dotnet/utilities/parameters/](https://docs.powertools.aws.dev/lambda/dotnet/utilities/parameters/)

GitHub: <https://github.com/aws-powertools/powertools-lambda-dotnet/>

## Sample Function using AWS Systems Manager Parameter Store

```csharp
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

namespace HelloWorld;

public class Function
{
    [Logging(LogEvent = true)]
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigwProxyEvent,
        ILambdaContext context)
    {    
        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider;

        // Retrieve a single parameter
        string? value = await ssmProvider
            .GetAsync("/my/parameter")
            .ConfigureAwait(false);

        // Retrieve multiple parameters from a path prefix
        // This returns a Dictionary with the parameter name as key
        IDictionary<string, string?> values = await ssmProvider
            .GetMultipleAsync("/my/path/prefix")
            .ConfigureAwait(false);

        ...

    }
}
```

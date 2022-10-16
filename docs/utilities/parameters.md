---
title: Parameters
description: Utility
---

<!-- markdownlint-disable MD013 -->
The parameters utility provides high-level functions to retrieve one or multiple parameter values from [AWS Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html){target="_blank"}, [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/){target="_blank"}, [AWS AppConfig](https://docs.aws.amazon.com/appconfig/latest/userguide/what-is-appconfig.html){target="_blank"}, [Amazon DynamoDB](https://aws.amazon.com/dynamodb/){target="_blank"}, or bring your own.

## Key features

* Retrieve one or multiple parameters from the underlying provider
* Cache parameter values for a given amount of time (defaults to 5 seconds)
* Transform parameter values from JSON or base 64 encoded strings
* Bring Your Own Parameter Store Provider

## Install

Powertools are available as NuGet packages. You can install the packages from NuGet gallery or from Visual Studio editor. Search `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Parameters](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Parameters):

    `dotnet nuget add AWS.Lambda.Powertools.Parameters`

**IAM Permissions**

This utility requires additional permissions to work as expected. See the table below:

Provider | Function/Method | IAM Permission
------------------------------------------------- | ------------------------------------------------- | ---------------------------------------------------------------------------------
SSM Parameter Store | `SsmProvider.Get(string)` `SsmProvider.Get<T>(string)` | `ssm:GetParameter`
SSM Parameter Store | `SsmProvider.GetMultiple(string)` `SsmProvider.GetMultiple<T>(string)` | `ssm:GetParametersByPath`
Secrets Manager | `SecretsProvider.Get(string)` `SecretsProvider.Get<T>(string)` | `secretsmanager:GetSecretValue`
DynamoDB | `DynamoDBProvider.Get(string)` `DynamoDBProvider.Get<T>(string)` | `dynamodb:GetItem`
DynamoDB | `DynamoDBProvider.GetMultiple(string)` `DynamoDBProvider.GetMultiple<T>(string)` | `dynamodb:Query`
App Config | `DynamoDBProvider.Get()` | `appconfig:StartConfigurationSession appconfig:GetLatestConfiguration`

## SSM Parameter Store

You can retrieve a single parameter using SsmProvider.Get() and pass the key of the parameter.
For multiple parameters, you can use SsmProvider.GetMultiple() and pass the path to retrieve them all.

Alternatively, you can retrieve the instance of provider and configure its underlying SDK client,
in order to get data from other regions or use specific credentials.

=== "SsmProvider"

    ```c# hl_lines="12-14 18-20"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
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
        }
    }
    ```
    
=== "SsmProvider with an explicit region"

    ```c# hl_lines="9-12 16-19"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider
                .ConfigureClient(RegionEndpoint.EUCentral1);
                
            // Retrieve a single parameter
            string? value = await ssmProvider
                .GetAsync("/my/parameter")
                .ConfigureAwait(false);
            
            // Retrieve multiple parameters from a path prefix
            // This returns a Dictionary with the parameter name as key
            IDictionary<string, string?> values = await ssmProvider
                .GetMultipleAsync("/my/path/prefix")
                .ConfigureAwait(false);
        }
    }
    ```

=== "SsmProvider with a custom client"

    ```c# hl_lines="9-12 16-19"
    using Amazon.SimpleSystemsManagement;
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Create a new instance of client
            IAmazonSimpleSystemsManagement client = new AmazonSimpleSystemsManagementClient();

            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider
                .UseClient(client);
                
            // Retrieve a single parameter
            string? value = await ssmProvider
                .GetAsync("/my/parameter")
                .ConfigureAwait(false);
            
            // Retrieve multiple parameters from a path prefix
            // This returns a Dictionary with the parameter name as key
            IDictionary<string, string?> values = await ssmProvider
                .GetMultipleAsync("/my/path/prefix")
                .ConfigureAwait(false);
        }
    }
    ```

### Additional arguments

The AWS Systems Manager Parameter Store provider supports two additional arguments for the `Get()` and `GetMultiple()` methods:

| Option     | Default | Description |
|---------------|---------|-------------|
| **WithDecryption()**   | `False` | Will automatically decrypt the parameter. |
| **Recursive()** | `False`  | For `GetMultiple()` only, will fetch all parameter values recursively based on a path prefix. |

**Example:**

=== "Function.cs"

    ```c# hl_lines="12-14 18-20"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider;
                
            // Retrieve a single parameter
            string? value = await ssmProvider
                .WithDecryption()
                .GetAsync("/my/parameter")
                .ConfigureAwait(false);
            
            // Retrieve multiple parameters from a path prefix
            // This returns a Dictionary with the parameter name as key
            IDictionary<string, string?> values = await ssmProvider
                .Recursive()
                .GetMultipleAsync("/my/path/prefix")
                .ConfigureAwait(false);
        }
    }
    ```

## Secrets Manager

For secrets stored in Secrets Manager, use `SecretsProvider`.

Alternatively, you can retrieve the instance of provider and configure its underlying SDK client,
in order to get data from other regions or use specific credentials.


=== "SecretsProvider"

     ```c# hl_lines="12-14 18-20"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SecretsManager;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get Secrets Provider instance
            ISecretsProvider secretsProvider = ParametersManager.SecretsProvider;
                
            // Retrieve a single secret
            string? value = await secretsProvider
                .GetAsync("/my/secret")
                .ConfigureAwait(false);
        }
    }
    ```
=== "SecretsProvider with an explicit region"

    ```c# hl_lines="9-12 16-19"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SecretsManager;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get Secrets Provider instance
            ISecretsProvider secretsProvider = ParametersManager.SecretsProvider
                .ConfigureClient(RegionEndpoint.EUCentral1);
                
            // Retrieve a single secret
            string? value = await secretsProvider
                .GetAsync("/my/secret")
                .ConfigureAwait(false);
        }
    }
    ```

=== "SecretsProvider with a custom clieent"

    ```c# hl_lines="9-12 16-19"
    using Amazon.SecretsManager;
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SecretsManager;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
             // Create a new instance of client
            IAmazonSecretsManager client = new AmazonSecretsManagerClient(); 

            // Get Secrets Provider instance
            ISecretsProvider secretsProvider = ParametersManager.SecretsProvider
                .UseClient(client);
                
            // Retrieve a single secret
            string? value = await secretsProvider
                .GetAsync("/my/secret")
                .ConfigureAwait(false);
        }
    }
    ```


## App Configurations

For application configurations in AWS AppConfig, use `AppConfigProvider`.

Alternatively, you can retrieve the instance of provider and configure its underlying SDK client,
in order to get data from other regions or use specific credentials.


=== "AppConfigProvider"

     ```c# hl_lines="12-14 18-20"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.AppConfig;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get AppConfig Provider instance
            IAppConfigProvider appConfigProvider = ParametersManager.AppConfigProvider
                .DefaultApplication("MyApplicationId")
                .DefaultEnvironment("MyEnvironmentId")
                .DefaultConfigProfile("MyConfigProfileId");
                
            // Retrieve a single configuration, latest version
            IDictionary<string, string?> value = await appConfigProvider
                .GetAsync()
                .ConfigureAwait(false);
        }
    }
    ```

=== "AppConfigProvider with an explicit region"

     ```c# hl_lines="12-14 18-20"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.AppConfig;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get AppConfig Provider instance
            IAppConfigProvider appConfigProvider = ParametersManager.AppConfigProvider
                .ConfigureClient(RegionEndpoint.EUCentral1)
                .DefaultApplication("MyApplicationId")
                .DefaultEnvironment("MyEnvironmentId")
                .DefaultConfigProfile("MyConfigProfileId");
                
            // Retrieve a single configuration, latest version
            IDictionary<string, string?> value = await appConfigProvider
                .GetAsync()
                .ConfigureAwait(false);
        }
    }
    ```

## DynamoDB Parameter Store

For parameters stored in a DynamoDB table, use `DynamoDBProvider`.

**DynamoDB table structure for single parameters**

For single parameters, you must use `id` as the [partition key](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.CoreComponents.html#HowItWorks.CoreComponents.PrimaryKey) for that table.

???+ example

	DynamoDB table with `id` partition key and `value` as attribute

 | id           | value    |
 | ------------ | -------- |
 | my-parameter | my-value |

With this table, `DynamoDBProvider.Get("my-param")` will return `my-value`.


=== "DynamoDBProvider"

     ```c# hl_lines="12-14 18-20"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.DynamoDB;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get DynamoDB Provider instance
            IDynamoDBProvider dynamoDbProvider = ParametersManager.DynamoDBProvider
                .UseTable
                (
                    "my-table",  // DynamoDB table name, required
                    "id",        // Partition Key attribute name, optional, default is 'id'
                    "value"      // Value attribute name, optional, default is 'value'
                );
                
            // Retrieve a single parameter
            string? value = await dynamoDbProvider
                .GetAsync("my-param")
                .ConfigureAwait(false);
        }
    }
    ```

**DynamoDB table structure for multiple values parameters**

You can retrieve multiple parameters sharing the same `id` by having a sort key named `sk`.

???+ example

	DynamoDB table with `id` primary key, `sk` as sort key` and `value` as attribute

 | id          | sk      | value      |
 | ----------- | ------- | ---------- |
 | my-hash-key | param-a | my-value-a |
 | my-hash-key | param-b | my-value-b |
 | my-hash-key | param-c | my-value-c |

With this table, `DynamoDBProvider.GetMultiple("my-hash-key")` will return a dictionary response in the shape of `sk:value`.

=== "DynamoDBProvider"

     ```c# hl_lines="12-14 18-20"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.DynamoDB;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get DynamoDB Provider instance
            IDynamoDBProvider dynamoDbProvider = ParametersManager.DynamoDBProvider
                .UseTable("my-table");
                
            // Retrieve a single parameter
            IDictionary<string, string?> value = await dynamoDbProvider
                .GetAsync("my-hash-key")
                .ConfigureAwait(false);
        }
    }
    ```

=== "parameters dictionary response"

	```json
	{
		"param-a": "my-value-a",
		"param-b": "my-value-b",
		"param-c": "my-value-c"
	}

**Customizing DynamoDBProvider**

DynamoDB provider can be customized at initialization to match your table structure:

| Parameter      | Mandatory | Default | Description                                                                                                |
| -------------- | --------- | ------- | ---------------------------------------------------------------------------------------------------------- |
| **table_name** | **Yes**   | *(N/A)* | Name of the DynamoDB table containing the parameter values.                                                |
| **key_attr**   | No        | `id`    | Hash key for the DynamoDB table.                                                                           |
| **sort_attr**  | No        | `sk`    | Range key for the DynamoDB table. You don't need to set this if you don't use the `GetMultiple()` method. |
| **value_attr** | No        | `value` | Name of the attribute containing the parameter value.                

=== "DynamoDBProvider"

     ```c# hl_lines="12-14 18-20"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.DynamoDB;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get DynamoDB Provider instance
            IDynamoDBProvider dynamoDbProvider = ParametersManager.DynamoDBProvider
                .UseTable
                (
                    tableName: "TableName",    // DynamoDB table name, Required.
                    primaryKeyAttribute: "id", // Partition Key attribute name, optional, default is 'id'
                    sortKeyAttribute: "sk",    // Sort Key attribute name, optional, default is 'sk'
                    valueAttribute: "value"    // Value attribute name, optional, default is 'value'
                );
        }
    }
    ```

## Advanced configuration

### Caching

By default, all parameters and their corresponding values are cached for 5 seconds.

You can customize this default value using `DefaultMaxAge`. You can also customize this value for each parameter using 
`WithMaxAge`. 

If you'd like to always ensure you fetch the latest parameter from the store regardless if already available in cache, use `ForceFetch`.

=== "Provider with default Max age"

    ```c# hl_lines="9"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider
                .DefaultMaxAge(TimeSpan.FromSeconds(10));
                
            // Retrieve a single parameter
            string? value = await ssmProvider
                .GetAsync("/my/parameter")
                .ConfigureAwait(false);
        }
    }
    ```

=== "Provider with age for each parameter"

    ```c# hl_lines="9"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider;
                
            // Retrieve a single parameter
            string? value = await ssmProvider
                .WithMaxAge(TimeSpan.FromSeconds(10))
                .GetAsync("/my/parameter")
                .ConfigureAwait(false);
        }
    }
    ```

=== "Force to fetch the latest parameter"

    ```c# hl_lines="9"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider;
                
            // Retrieve a single parameter
            string? value = await ssmProvider
                .ForceFetch()
                .GetAsync("/my/parameter")
                .ConfigureAwait(false);
        }
    }
    ```

### Transform values

Parameter values can be transformed using ```WithTransformation(TransformationEnum)```. Base64 and JSON transformations are provided.
For more complex transformation, you need to specify how to deserialize by writing your own transfomer.

=== "JSON Transformation"

    ```c# hl_lines="9"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider;
                
            // Retrieve a single parameter
            var value = await ssmProvider
                .WithTransformation(Transformation.Json)
                .GetAsync<MyObj>("/my/parameter/json")
                .ConfigureAwait(false);
        }
    }
    ```

=== "Base64 Transformation"

    ```c# hl_lines="9"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider;
                
            // Retrieve a single parameter
            var value = await ssmProvider
                .WithTransformation(Transformation.Base64)
                .GetAsync("/my/parameter/b64")
                .ConfigureAwait(false);
        }
    }
    ```

#### Partial transform failures with `GetMultiple()`

If you use `Transformation` with `GetMultiple()`, you can have a single malformed parameter value. To prevent failing the entire request, the method will return a `Null` value for the parameters that failed to transform.

You can override this by using ```RaiseTransformationError()```. If you do so, a single transform error will raise a **`TransformationException`** exception.

=== "Function.cs"

    ```c# hl_lines="9"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider
                .RaiseTransformationError();
                
            // Retrieve a single parameter
            var value = await ssmProvider
                .WithTransformation(Transformation.Json)
                .GetAsync<MyObj>("/my/parameter/json")
                .ConfigureAwait(false);
        }
    }
    ```

#### Auto-transform values on suffix

If you use `Transformation` with `GetMultiple()`, you might want to retrieve and transform parameters encoded in different formats.

You can do this with a single request by using `Transformation.Auto`. This will instruct any Parameter to to infer its type based on the suffix and transform it accordingly.
    
=== "Function.cs"

    ```c# hl_lines="9"
    using AWS.Lambda.Powertools.Parameters;
    using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler
            (APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            // Get SSM Provider instance
            ISsmProvider ssmProvider = ParametersManager.SsmProvider;
                
            // Retrieve multiple parameters from a path prefix
            // This returns a Dictionary with the parameter name as key
            IDictionary<string, object?> values = await ssmProvider
                .WithTransformation(Transformation.Auto)
                .GetMultipleAsync("/param")
                .ConfigureAwait(false);
        }
    }
    ```

For example, if you have two parameters with the following suffixes `.json` and `.binary`:

| Parameter name  | Parameter value      |
| --------------- | -------------------- |
| /param/a.json   | [some encoded value] |
| /param/a.binary | [some encoded value] |

The return of `GetMultiple()` with `Transformation.Auto` will be a dictionary like:

```json
{
    "a.json": [some value],
    "b.binary": [some value]
}
```

## Write your own Transformer

You can write your own transformer, by implementing the `ITransformer` interface and the `Transform<T>(string)` method.
For example, if you wish to deserialize XML into an object.

=== "XmlTransformer.cs"

    ```c# hl_lines="1"
    public class XmlTransformer : ITransformer
    {
        public T? Transform<T>(string value)
        {
            if (string.IsNullOrEmpty(value))
                return default;
            
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(value);
            return (T?)serializer.Deserialize(reader);
        }
    }
    ```

=== "Using XmlTransformer"

    ```c#
        var value = await ssmProvider
            .WithTransformation(new XmlTransformer())
            .GetAsync<MyObj>("/my/parameter/xml")
            .ConfigureAwait(false);
    ```

=== "Adding XmlTransformer as transformer"

    ```c#
        // Get SSM Provider instance
        ISsmProvider ssmProvider = ParametersManager.SsmProvider
            .AddTransformer("XML", new XmlTransformer());

        // Retrieve a single parameter
        var value = await ssmProvider
            .WithTransformation("XML")
            .GetAsync<MyObj>("/my/parameter/xml")
            .ConfigureAwait(false);
    ```

### Fluent API

To simplify the use of the library, you can chain all method calls before a get.

=== "Fluent API call"

    ```java
        ssmProvider
          .DefaultMaxAge(TimeSpan.FromSeconds(10))  // will set 10 seconds as the default cache TTL
          .WithMaxAge(TimeSpan.FromMinutes(1))      // will set the cache TTL for this value at 1 minute
          .WithTransformation(Transformation.Json)  // Will use JSON transfomer to deserializes JSON to an object
          .WithDecryption()                         // enable decryption of the parameter value
          .Get<MyObj>("/my/param");                 // finally get the value
    ```

## Create your own provider

You can create your own custom parameter provider by inheriting the ```BaseProvider``` class and implementing the
```String getValue(String key)``` method to retrieve data from your underlying store. All transformation and caching logic is handled by the get() methods in the base class.

=== "Example implementation using S3 as a custom parameter"

    ```c#
    public class S3Provider : ParameterProvider
    {
    
        private string _bucket;
        private readonly IAmazonS3 _client;
    
        public S3Provider()
        {
            _client = new AmazonS3Client();
        }

        public S3Provider(IAmazonS3 client)
        {
            _client = client;
        }
    
        public S3Provider WithBucket(string bucket)
        {
            _bucket = bucket;
            return this;
        }
    
        protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (string.IsNullOrEmpty(_bucket))
                throw new ArgumentException("A bucket must be specified, using withBucket() method");

            var request = new GetObjectRequest
            {
                Key = key,
                BucketName = _bucket
            };

            using var response = await _client.GetObjectAsync(request);
            await using var responseStream = response.ResponseStream;
            using var reader = new StreamReader(responseStream);
            return await reader.ReadToEndAsync();
        }
    
         protected override async Task<IDictionary<string, string?>> GetMultipleAsync(string path, ParameterProviderConfiguration? config)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (string.IsNullOrEmpty(_bucket))
                throw new ArgumentException("A bucket must be specified, using withBucket() method");

            var request = new ListObjectsV2Request
            {
                Prefix = path,
                BucketName = _bucket
            };
            var response = await _client.ListObjectsV2Async(request);

            var result = new Dictionary<string, string?>();
            foreach (var s3Object in response.S3Objects)
            {
                var value = await GetAsync(s3Object.Key);
                result.Add(s3Object.Key, value);
            }

            return result;
        }
    }
    ```

=== "Using custom parameter store"

    ```c# hl_lines="3"
        var provider = new S3Provider();
        
        var value = await provider
            .WithBucket("myBucket")
            .GetAsync("myKey")
            .ConfigureAwait(false);
    ```


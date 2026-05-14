---
title: Parameters
description: Utility
---

<!-- markdownlint-disable MD013 -->
The Parameters utility provides high-level functionality to retrieve one or multiple parameter values from [AWS Systems Manager Parameter Store](https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html){target="_blank"}, [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/){target="_blank"}, [Amazon DynamoDB](https://aws.amazon.com/dynamodb/){target="_blank"}, or [AWS AppConfig](https://docs.aws.amazon.com/appconfig/latest/userguide/what-is-appconfig.html){target="_blank"}. We also provide extensibility to bring your own providers.

## Key features

* Retrieve one or multiple parameters from the underlying provider
* Cache parameter values for a given amount of time (defaults to 5 seconds)
* Transform parameter values from JSON or base 64 encoded strings
* Bring your own parameter store provider

!!! warning "Migrating to v3"

    If you're upgrading to v3, please review the [Migration Guide v3](../migration-guide-v3.md) for important breaking changes including .NET 8 requirement and AWS SDK v4 migration.

## Installation

Powertools for AWS Lambda (.NET) are available as NuGet packages. You can install the packages from [NuGet Gallery](https://www.nuget.org/packages?q=AWS+Lambda+Powertools*){target="_blank"} or from Visual Studio editor by searching `AWS.Lambda.Powertools*` to see various utilities available.

* [AWS.Lambda.Powertools.Parameters](https://www.nuget.org/packages?q=AWS.Lambda.Powertools.Parameters):

    `dotnet nuget add AWS.Lambda.Powertools.Parameters`

**IAM Permissions**

This utility requires additional permissions to work as expected. See the table below:

| Provider            | Function/Method                                                                  | IAM Permission                                          |
| ------------------- | -------------------------------------------------------------------------------- | ------------------------------------------------------- |
| SSM Parameter Store | `SsmProvider.Get(string)` `SsmProvider.Get<T>(string)`                           | `ssm:GetParameter`                                      |
| SSM Parameter Store | `SsmProvider.GetMultiple(string)` `SsmProvider.GetMultiple<T>(string)`           | `ssm:GetParametersByPath`                               |
| SSM Parameter Store | If using **`WithDecryption()`** option                                           | You must add an additional permission `kms:Decrypt`     |
| Secrets Manager     | `SecretsProvider.Get(string)` `SecretsProvider.Get<T>(string)`                   | `secretsmanager:GetSecretValue`                         |
| DynamoDB            | `DynamoDBProvider.Get(string)` `DynamoDBProvider.Get<T>(string)`                 | `dynamodb:GetItem`                                      |
| DynamoDB            | `DynamoDBProvider.GetMultiple(string)` `DynamoDBProvider.GetMultiple<T>(string)` | `dynamodb:Query`                                        |
| App Config          | `AppConfigProvider.Get()`       | `appconfig:StartConfigurationSession` `appconfig:GetLatestConfiguration`                      |

## SSM Parameter Store

You can retrieve a single parameter using `SsmProvider.Get()` and pass the key of the parameter.
For multiple parameters, you can use `SsmProvider.GetMultiple()` and pass the path to retrieve them all.

Alternatively, you can retrieve the instance of provider and configure its underlying SDK client,
in order to get data from other regions or use specific credentials.

=== "SsmProvider"

    ```csharp hl_lines="10"
    --8<-- "docs/snippets/parameters/SsmProvider.cs:ssm_provider"
    ```
    
=== "SsmProvider with an explicit region"

    ```csharp hl_lines="10 11"
    --8<-- "docs/snippets/parameters/SsmProvider.cs:ssm_provider_explicit_region"
    ```

=== "SsmProvider with a custom client"

    ```csharp hl_lines="11 14 15"
    --8<-- "docs/snippets/parameters/SsmProvider.cs:ssm_provider_custom_client"
    ```

### Additional arguments

The AWS Systems Manager Parameter Store provider supports two additional arguments for the `Get()` and `GetMultiple()` methods:

| Option               | Default | Description                                                                                   |
| -------------------- | ------- | --------------------------------------------------------------------------------------------- |
| **WithDecryption()** | `False` | Will automatically decrypt the parameter.                                                     |
| **Recursive()**      | `False` | For `GetMultiple()` only, will fetch all parameter values recursively based on a path prefix. |

You can create `SecureString` parameters, which are parameters that have a plaintext parameter name and an encrypted parameter value. If you don't use the `WithDecryption()` option, you will get an encrypted value. Read [here](https://docs.aws.amazon.com/kms/latest/developerguide/services-parameter-store.html) about best practices using KMS to secure your parameters.

**Example:**

=== "Function.cs"

    ```csharp hl_lines="13-16 20-23"
    --8<-- "docs/snippets/parameters/SsmProvider.cs:ssm_provider_additional_args"
    ```

## Secrets Manager

For secrets stored in Secrets Manager, use `SecretsProvider`.

Alternatively, you can retrieve the instance of provider and configure its underlying SDK client,
in order to get data from other regions or use specific credentials.

=== "SecretsProvider"

    ```csharp hl_lines="13-15"
    --8<-- "docs/snippets/parameters/SecretsProvider.cs:secrets_provider"
    ```
=== "SecretsProvider with an explicit region"

    ```csharp hl_lines="10-11 14-16"
    --8<-- "docs/snippets/parameters/SecretsProvider.cs:secrets_provider_explicit_region"
    ```

=== "SecretsProvider with a custom client"

    ```csharp hl_lines="11 14 15"
    --8<-- "docs/snippets/parameters/SecretsProvider.cs:secrets_provider_custom_client"
    ```

## DynamoDB Provider

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

    ```csharp hl_lines="10 11 14-16"
    --8<-- "docs/snippets/parameters/DynamoDbProvider.cs:dynamodb_provider_single"
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

    ```csharp hl_lines="10 11 14-16"
    --8<-- "docs/snippets/parameters/DynamoDbProvider.cs:dynamodb_provider_multiple"
    ```

=== "parameters dictionary response"

	```json
	{
		"param-a": "my-value-a",
		"param-b": "my-value-b",
		"param-c": "my-value-c"
	}
    ```

**Customizing DynamoDBProvider**

DynamoDB provider can be customized at initialization to match your table structure:

| Parameter      | Mandatory | Default | Description                                                                                                |
| -------------- | --------- | ------- | ---------------------------------------------------------------------------------------------------------- |
| **table_name** | **Yes**   | *(N/A)* | Name of the DynamoDB table containing the parameter values.                                                |
| **key_attr**   | No        | `id`    | Hash key for the DynamoDB table.                                                                           |
| **sort_attr**  | No        | `sk`    | Range key for the DynamoDB table. You don't need to set this if you don't use the `GetMultiple()` method.  |
| **value_attr** | No        | `value` | Name of the attribute containing the parameter value.                                                      |

=== "DynamoDBProvider"

    ```csharp hl_lines="10-17"
    --8<-- "docs/snippets/parameters/DynamoDbProvider.cs:dynamodb_provider_customizing"
    ```

## App Configurations

For application configurations in AWS AppConfig, use `AppConfigProvider`.

Alternatively, you can retrieve the instance of provider and configure its underlying SDK client,
in order to get data from other regions or use specific credentials.

=== "AppConfigProvider"

    ```csharp hl_lines="10-13 16-18"
    --8<-- "docs/snippets/parameters/AppConfigProvider.cs:app_config_provider"
    ```

=== "AppConfigProvider with an explicit region"

    ```csharp hl_lines="10-14"
    --8<-- "docs/snippets/parameters/AppConfigProvider.cs:app_config_provider_explicit_region"
    ```

**Using AWS AppConfig Feature Flags**

Feature flagging is a powerful tool that allows safely pushing out new features in a measured and usually gradual way. AppConfig provider offers helper methods to make it easier to work with feature flags.

=== "AppConfigProvider"

    ```csharp hl_lines="10-13 16-18 23-25"
    --8<-- "docs/snippets/parameters/AppConfigProvider.cs:app_config_feature_flags"
    ```

## Advanced configuration

### Caching

By default, all parameters and their corresponding values are cached for 5 seconds.

You can customize this default value using `DefaultMaxAge`. You can also customize this value for each parameter using 
`WithMaxAge`.

If you'd like to always ensure you fetch the latest parameter from the store regardless if already available in cache, use `ForceFetch`.

=== "Provider with default Max age"

    ```csharp hl_lines="10 11"
    --8<-- "docs/snippets/parameters/Caching.cs:default_max_age"
    ```

=== "Provider with age for each parameter"

    ```csharp hl_lines="13-16"
    --8<-- "docs/snippets/parameters/Caching.cs:max_age_per_parameter"
    ```

=== "Force to fetch the latest parameter"

    ```csharp hl_lines="13-16"
    --8<-- "docs/snippets/parameters/Caching.cs:force_fetch"
    ```

### Transform values

Parameter values can be transformed using ```WithTransformation()```. Base64 and JSON transformations are provided.
For more complex transformation, you need to specify how to deserialize by writing your own transfomer.

=== "JSON Transformation"

    ```csharp hl_lines="13-16"
    --8<-- "docs/snippets/parameters/TransformValues.cs:json_transformation"
    ```

=== "Base64 Transformation"

    ```csharp hl_lines="13-16"
    --8<-- "docs/snippets/parameters/TransformValues.cs:base64_transformation"
    ```

#### Partial transform failures with `GetMultiple()`

If you use `Transformation` with `GetMultiple()`, you can have a single malformed parameter value. To prevent failing the entire request, the method will return a `Null` value for the parameters that failed to transform.

You can override this by using ```RaiseTransformationError()```. If you do so, a single transform error will raise a **`TransformationException`** exception.

=== "Function.cs"

    ```csharp hl_lines="10 11"
    --8<-- "docs/snippets/parameters/TransformValues.cs:raise_transformation_error"
    ```

#### Auto-transform values on suffix

If you use `Transformation` with `GetMultiple()`, you might want to retrieve and transform parameters encoded in different formats.

You can do this with a single request by using `Transformation.Auto`. This will instruct any Parameter to to infer its type based on the suffix and transform it accordingly.
    
=== "Function.cs"

    ```csharp hl_lines="14-17"
    --8<-- "docs/snippets/parameters/TransformValues.cs:auto_transform"
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

    ```csharp hl_lines="1 3"
    --8<-- "docs/snippets/parameters/CustomTransformer.cs:xml_transformer"
    ```

=== "Using XmlTransformer"

    ```csharp
    --8<-- "docs/snippets/parameters/CustomTransformer.cs:using_xml_transformer"
    ```

=== "Adding XmlTransformer as transformer"

    ```csharp hl_lines="2 3 7"
    --8<-- "docs/snippets/parameters/CustomTransformer.cs:adding_xml_transformer"
    ```

### Fluent API

To simplify the use of the library, you can chain all method calls before a get.

=== "Fluent API call"

    ```csharp
    --8<-- "docs/snippets/parameters/CustomTransformer.cs:fluent_api"
    ```

## Create your own provider

You can create your own custom parameter provider by inheriting the ```BaseProvider``` class and implementing the
```String getValue(String key)``` method to retrieve data from your underlying store. All transformation and caching logic is handled by the get() methods in the base class.

=== "Example implementation using S3 as a custom parameter"

    ```csharp
    --8<-- "docs/snippets/parameters/CustomProvider.cs:s3_provider"
    ```

=== "Using custom parameter store"

    ```csharp
    --8<-- "docs/snippets/parameters/CustomProvider.cs:using_custom_provider"
    ```


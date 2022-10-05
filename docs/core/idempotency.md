---
title: Idempotency
description: Utility
---

The idempotency utility provides a simple solution to convert your Lambda functions into idempotent operations which are safe to retry.

## Terminology

The property of idempotency means that an operation does not cause additional side effects if it is called more than once with the same input parameters.

**Idempotent operations will return the same result when they are called multiple times with the same parameters**. This makes idempotent operations safe to retry. [Read more](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/) about idempotency.

**Idempotency key** is a hash representation of either the entire event or a specific configured subset of the event, and invocation results are **JSON serialized** and stored in your persistence storage layer.

## Key features

* Prevent Lambda handler function from executing more than once on the same event payload during a time window
* Ensure Lambda handler returns the same result when called with the same payload
* Select a subset of the event as the idempotency key using [JMESPath](https://jmespath.org/) expressions
* Set a time window in which records with the same payload should be considered duplicates

## Getting started

### Installation
You should install with NuGet:

```
Install-Package AWS.Lambda.Powertools.Idempotency
```

Or via the .NET Core command line interface:

```
dotnet add package AWS.Lambda.Powertools.Idempotency
```

### Required resources

Before getting started, you need to create a persistent storage layer where the idempotency utility can store its state - your Lambda functions will need read and write access to it.

As of now, Amazon DynamoDB is the only supported persistent storage layer, so you'll need to create a table first.

**Default table configuration**

If you're not [changing the default configuration for the DynamoDB persistence layer](#dynamodbpersistencestore), this is the expected default configuration:

| Configuration      | Value        | Notes                                                                               |
|--------------------|--------------|-------------------------------------------------------------------------------------|
| Partition key      | `id`         |                                                                                     |
| TTL attribute name | `expiration` | This can only be configured after your table is created if you're using AWS Console |

!!! Tip "Tip: You can share a single state table for all functions"
    You can reuse the same DynamoDB table to store idempotency state. We add your function name in addition to the idempotency key as a hash key.

=== "template.yml"

    ```yaml hl_lines="5-13 21-23 26" title="AWS Serverless Application Model (SAM) example"
    Resources:
    IdempotencyTable:
        Type: AWS::DynamoDB::Table
        Properties:
        AttributeDefinitions:
            - AttributeName: id
            AttributeType: S
        KeySchema:
            - AttributeName: id
            KeyType: HASH
        TimeToLiveSpecification:
            AttributeName: expiration
            Enabled: true
        BillingMode: PAY_PER_REQUEST

    IdempotencyFunction:
        Type: AWS::Serverless::Function
        Properties:
        CodeUri: Function
        Handler: HelloWorld::HelloWorld.Function::FunctionHandler
        Policies:
            - DynamoDBCrudPolicy:
                TableName: !Ref IdempotencyTable
        Environment:
            Variables:
            IDEMPOTENCY_TABLE: !Ref IdempotencyTable
    ```

!!! warning "Warning: Large responses with DynamoDB persistence layer"
    When using this utility with DynamoDB, your function's responses must be [smaller than 400KB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Limits.html#limits-items).
    Larger items cannot be written to DynamoDB and will cause exceptions.

!!! info "Info: DynamoDB"
    Each function invocation will generally make 2 requests to DynamoDB. If the
    result returned by your Lambda is less than 1kb, you can expect 2 WCUs per invocation. For retried invocations, you will
    see 1WCU and 1RCU. Review the [DynamoDB pricing documentation](https://aws.amazon.com/dynamodb/pricing/) to
    estimate the cost.

### Idempotent attribute

You can quickly start by configuring `Idempotency` and using it with the `Idempotent` attribute on your Lambda function.

!!! warning "Important"
    Initialization and configuration of the `Idempotency` must be performed outside the handler, preferably in the constructor.

    ```csharp
    public class Function
    {
        public Function()
        {
            Idempotency.Configure(builder => builder.UseDynamoDb("idempotency_table"));
        }
        
        [Idempotent]
        public Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            return Task.FromResult(input.ToUpper());
        }
    }
    ```

### Choosing a payload subset for idempotency

!!! tip "Tip: Dealing with always changing payloads"
    When dealing with an elaborate payload (API Gateway request for example), where parts of the payload always change, you should configure the **`EventKeyJmesPath`**.

Use [`IdempotencyConfig`](#customizing-the-default-behavior) to instruct the Idempotent annotation to only use a portion of your payload to verify whether a request is idempotent, and therefore it should not be retried.

> **Payment scenario**

In this example, we have a Lambda handler that creates a payment for a user subscribing to a product. We want to ensure that we don't accidentally charge our customer by subscribing them more than once.

Imagine the function executes successfully, but the client never receives the response due to a connection issue. It is safe to retry in this instance, as the idempotent decorator will return a previously saved response.

!!! warning "Warning: Idempotency for JSON payloads"
    The payload extracted by the `EventKeyJmesPath` is treated as a string by default, so will be sensitive to differences in whitespace even when the JSON payload itself is identical.

    To alter this behaviour, you can use the JMESPath built-in function `powertools_json()` to treat the payload as a JSON object rather than a string.

    ```csharp
    Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder.WithEventKeyJmesPath("powertools_json(Body).address"))
                .UseDynamoDb("idempotency_table"));
    ```

### Handling exceptions

If you are using the `Idempotent` attribute on your Lambda handler or any other method, any unhandled exceptions that are thrown during the code execution will cause **the record in the persistence layer to be deleted**.
This means that new invocations will execute your code again despite having the same payload. If you don't want the record to be deleted, you need to catch exceptions within the idempotent function and return a successful response.

!!! warning
    **We will throw an `IdempotencyPersistenceLayerException`** if any of the calls to the persistence layer fail unexpectedly.

    As this happens outside the scope of your decorated function, you are not able to catch it.

### Persistence stores

#### DynamoDBPersistenceStore

This persistence store is built-in, and you can either use an existing DynamoDB table or create a new one dedicated for idempotency state (recommended).

Use the builder to customize the table structure:
```csharp title="Customizing DynamoDBPersistenceStore to suit your table structure"
new DynamoDBPersistenceStoreBuilder()
    .WithTableName(System.getenv("TABLE_NAME"))
    .WithKeyAttr("idempotency_key")
    .WithExpiryAttr("expires_at")
    .WithStatusAttr("current_status")
    .WithDataAttr("result_data")
    .WithValidationAttr("validation_key")
    .Build()
```

When using DynamoDB as a persistence layer, you can alter the attribute names by passing these parameters when initializing the persistence layer:

| Parameter          | Required | Default                              | Description                                                                                            |
|--------------------|----------|--------------------------------------|--------------------------------------------------------------------------------------------------------|
| **TableName**      | Y        |                                      | Table name to store state                                                                              |
| **KeyAttr**        |          | `id`                                 | Partition key of the table. Hashed representation of the payload (unless **SortKeyAttr** is specified) |
| **ExpiryAttr**     |          | `expiration`                         | Unix timestamp of when record expires                                                                  |
| **StatusAttr**     |          | `status`                             | Stores status of the Lambda execution during and after invocation                                      |
| **DataAttr**       |          | `data`                               | Stores results of successfully idempotent methods                                                      |
| **ValidationAttr** |          | `validation`                         | Hashed representation of the parts of the event used for validation                                    |
| **SortKeyAttr**    |          |                                      | Sort key of the table (if table is configured with a sort key).                                        |
| **StaticPkValue**  |          | `idempotency#{LAMBDA_FUNCTION_NAME}` | Static value to use as the partition key. Only used when **SortKeyAttr** is set.                       |

## Advanced

### Customizing the default behavior

Idempotency behavior can be further configured with **`IdempotencyConfig`** using a builder:

```csharp
new IdempotencyConfigBuilder()
    .WithEventKeyJmesPath("id")
    .WithPayloadValidationJmesPath("paymentId")
    .WithThrowOnNoIdempotencyKey(true)
    .WithExpiration(TimeSpan.FromMinutes(1))
    .WithUseLocalCache(true)
    .WithHashFunction("MD5")
    .Build();
```

These are the available options for further configuration:

| Parameter                                         | Default | Description                                                                                                                      |
|---------------------------------------------------|---------|----------------------------------------------------------------------------------------------------------------------------------|
| **EventKeyJMESPath**                              | `""`    | JMESPath expression to extract the idempotency key from the event record.                                                        |
| **PayloadValidationJMESPath**                     | `""`    | JMESPath expression to validate whether certain parameters have changed in the event                                             |
| **ThrowOnNoIdempotencyKey**                       | `False` | Throw exception if no idempotency key was found in the request                                                                   |
| **ExpirationInSeconds**                           | 3600    | The number of seconds to wait before a record is expired                                                                         |
| **UseLocalCache**                                 | `false` | Whether to locally cache idempotency results (LRU cache)                                                                         |
| **HashFunction**                                  | `MD5`   | Algorithm to use for calculating hashes, as supported by `System.Security.Cryptography.HashAlgorithm` (eg. SHA1, SHA-256, ...)   |

These features are detailed below.

### Handling concurrent executions with the same payload

This utility will throw an **`IdempotencyAlreadyInProgressException`** if we receive **multiple invocations with the same payload while the first invocation hasn't completed yet**.

!!! info
    If you receive `IdempotencyAlreadyInProgressException`, you can safely retry the operation.

This is a locking mechanism for correctness. Since we don't know the result from the first invocation yet, we can't safely allow another concurrent execution.

### Using in-memory cache

**By default, in-memory local caching is disabled**, to avoid using memory in an unpredictable way. 

!!! warning Memory configuration of your function
    Be sure to configure the Lambda memory according to the number of records and the potential size of each record.

You can enable it as seen before with:
```csharp title="Enable local cache"
    new IdempotencyConfigBuilder()
        .WithUseLocalCache(true)
        .Build()
```
When enabled, we cache a maximum of 255 records in each Lambda execution environment

!!! note "Note: This in-memory cache is local to each Lambda execution environment"
    This means it will be effective in cases where your function's concurrency is low in comparison to the number of "retry" invocations with the same payload, because cache might be empty.


### Expiring idempotency records

!!! note
    By default, we expire idempotency records after **an hour** (3600 seconds).

In most cases, it is not desirable to store the idempotency records forever. Rather, you want to guarantee that the same payload won't be executed within a period of time.

You can change this window with the **`ExpirationInSeconds`** parameter:
```csharp title="Customizing expiration time"
new IdempotencyConfigBuilder()
    .WithExpiration(TimeSpan.FromMinutes(5))
    .Build()
```

Records older than 5 minutes will be marked as expired, and the Lambda handler will be executed normally even if it is invoked with a matching payload.

!!! note "Note: DynamoDB time-to-live field"
    This utility uses **`expiration`** as the TTL field in DynamoDB, as [demonstrated in the SAM example earlier](#required-resources).

### Payload validation

!!! question "Question: What if your function is invoked with the same payload except some outer parameters have changed?"
    Example: A payment transaction for a given productID was requested twice for the same customer, **however the amount to be paid has changed in the second transaction**.

By default, we will return the same result as it returned before, however in this instance it may be misleading; we provide a fail fast payload validation to address this edge case.

With **`PayloadValidationJMESPath`**, you can provide an additional JMESPath expression to specify which part of the event body should be validated against previous idempotent invocations

=== "Function.cs"

    ```csharp
    Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder
                        .WithEventKeyJmesPath("[userDetail, productId]")
                        .WithPayloadValidationJmesPath("amount"))
                .UseDynamoDb("TABLE_NAME"));
    ```

=== "Example Event 1"

    ```json hl_lines="8"
    {
        "userDetail": {
            "username": "User1",
            "user_email": "user@example.com"
        },
        "productId": 1500,
        "charge_type": "subscription",
        "amount": 500
    }
    ```

=== "Example Event 2"

    ```json hl_lines="8"
    {
        "userDetail": {
            "username": "User1",
            "user_email": "user@example.com"
        },
        "productId": 1500,
        "charge_type": "subscription",
        "amount": 1
    }
    ```

In this example, the **`userDetail`** and **`productId`** keys are used as the payload to generate the idempotency key, as per **`EventKeyJMESPath`** parameter.

!!! note
    If we try to send the same request but with a different amount, we will raise **`IdempotencyValidationException`**.

Without payload validation, we would have returned the same result as we did for the initial request. Since we're also returning an amount in the response, this could be quite confusing for the client.

By using **`withPayloadValidationJMESPath("amount")`**, we prevent this potentially confusing behavior and instead throw an Exception.

### Making idempotency key required

If you want to enforce that an idempotency key is required, you can set **`ThrowOnNoIdempotencyKey`** to `True`.

This means that we will throw **`IdempotencyKeyException`** if the evaluation of **`EventKeyJMESPath`** is `null`.

=== "Function.cs"

    ```csharp
    public App() 
    {
      Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder
                        // Requires "user"."uid" and "orderId" to be present
                        .WithEventKeyJmesPath("[user.uid, orderId]")
                        .WithThrowOnNoIdempotencyKey(true))
                .UseDynamoDb("TABLE_NAME"));
    }

    [Idempotent]
    public Task<OrderResult> FunctionHandler(Order input, ILambdaContext context)
    {
      // ...
    }
    ```

=== "Success Event"

    ```json
    {
        "user": {
            "uid": "BB0D045C-8878-40C8-889E-38B3CB0A61B1",
            "name": "Foo"
        },
        "orderId": 10000
    }
    ```

=== "Failure Event"

    Notice that `orderId` is now accidentally within `user` key

    ```json
    {
        "user": {
            "uid": "DE0D000E-1234-10D1-991E-EAC1DD1D52C8",
            "name": "Joe Bloggs",
            "orderId": 10000
        },
    }
    ```

### Customizing DynamoDB configuration

When creating the `DynamoDBPersistenceStore`, you can set a custom [`AmazonDynamoDBClient`](https://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/T_Amazon_DynamoDB_AmazonDynamoDBClient.htm) if you need to customize the configuration:

=== "Custom AmazonDynamoDBClient"

    ```csharp
    public Function()
    {
        AmazonDynamoDBClient customClient = new AmazonDynamoDBClient(RegionEndpoint.APSouth1);
      
        Idempotency.Configure(builder => 
            builder.UseDynamoDb(storeBuilder => 
                storeBuilder.
                    WithTableName("TABLE_NAME")
                    .WithDynamoDBClient(customClient)
            ));
    }
    ```

### Using a DynamoDB table with a composite primary key

When using a composite primary key table (hash+range key), use `SortKeyAttr` parameter when initializing your persistence store.

With this setting, we will save the idempotency key in the sort key instead of the primary key. By default, the primary key will now be set to `idempotency#{LAMBDA_FUNCTION_NAME}`.

You can optionally set a static value for the partition key using the `StaticPkValue` parameter.

```csharp title="Reusing a DynamoDB table that uses a composite primary key"
Idempotency.Configure(builder => 
    builder.UseDynamoDb(storeBuilder => 
        storeBuilder.
            WithTableName("TABLE_NAME")
            .WithSortKeyAttr("sort_key")
    ));
```

Data would then be stored in DynamoDB like this:

| id                           | sort_key                         | expiration | status      | data                                 |
|------------------------------|----------------------------------|------------|-------------|--------------------------------------|
| idempotency#MyLambdaFunction | 1e956ef7da78d0cb890be999aecc0c9e | 1636549553 | COMPLETED   | {"id": 12391, "message": "success"}  |
| idempotency#MyLambdaFunction | 2b2cdb5f86361e97b4383087c1ffdf27 | 1636549571 | COMPLETED   | {"id": 527212, "message": "success"} |
| idempotency#MyLambdaFunction | f091d2527ad1c78f05d54cc3f363be80 | 1636549585 | IN_PROGRESS |                                      |

## Testing your code

The idempotency utility provides several routes to test your code.

### Disabling the idempotency utility
When testing your code, you may wish to disable the idempotency logic altogether and focus on testing your business logic. To do this, you can set the environment variable `POWERTOOLS_IDEMPOTENCY_DISABLED` to true. 

## Extra resources

If you're interested in a deep dive on how Amazon uses idempotency when building our APIs, check out
[this article](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/).

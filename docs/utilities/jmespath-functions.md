---
title: JMESPath Functions
description: Utility
---

<!-- markdownlint-disable MD043 -->

???+ tip
    JMESPath is a query language for JSON used by AWS CLI, AWS Python SDK, and Powertools for AWS Lambda.

Built-in JMESPath functions to easily deserialize common encoded JSON payloads in Lambda functions.

## Key features

* Deserialize JSON from JSON strings, base64, and compressed data
* Use JMESPath to extract and combine data recursively
* Provides commonly used JMESPath expression with popular event sources

## Getting started

???+ tip
    All examples shared in this documentation are available within the [project repository](https://github.com/aws-powertools/powertools-lambda-dotnet/tree/develop/libraries/tests/AWS.Lambda.Powertools.JMESPath.Tests/JmesPathExamples.cs){target="_blank"}.

You might have events that contains encoded JSON payloads as string, base64, or even in compressed format. It is a common use case to decode and extract them partially or fully as part of your Lambda function invocation.

???+ info "Terminology"
    **Envelope** is the terminology we use for the **JMESPath expression** to extract your JSON object from your data input. We might use those two terms interchangeably.

### Extracting data

You can use the `JsonTransformer.Transform` function with any [JMESPath expression](https://jmespath.org/tutorial.html){target="_blank" rel="nofollow"}.

???+ tip
    Another common use case is to fetch deeply nested data, filter, flatten, and more.

=== "Transform"
    ```csharp hl_lines="1 2"
    var transformer = JsonTransformer.Parse("powertools_json(body).customerId");
    using var result = transformer.Transform(doc.RootElement);
    
    Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
    ```

=== "Payload"
    ```json
     {
         "body": "{\"customerId\":\"dd4649e6-2484-4993-acb8-0f9123103394\"}",
         "deeply_nested": [
             {
                 "some_data": [
                     1,
                     2,
                     3
                 ]
             }
         ]
     }
    ```

### Built-in envelopes

We provide built-in envelopes for popular AWS Lambda event sources to easily decode and/or deserialize JSON objects.

| Envelop             | JMESPath expression                                                         |
|---------------------|-----------------------------------------------------------------------------|
| API_GATEWAY_HTTP    | powertools_json(body)                                                       |
| API_GATEWAY_REST    | powertools_json(body)                                                       |
| CLOUDWATCH_LOGS     | awslogs.powertools_base64_gzip(data) &#124; powertools_json(@).logEvents[*] |
| KINESIS_DATA_STREAM | Records[*].kinesis.powertools_json(powertools_base64(data))                 |
| SNS                 | Records[*].Sns.Message &#124; powertools_json(@)                            |
| SQS                 | Records[*].powertools_json(body)                                            |

???+ tip "Using SNS?"
    If you don't require SNS metadata, enable [raw message delivery](https://docs.aws.amazon.com/sns/latest/dg/sns-large-payload-raw-message-delivery.html). It will reduce multiple payload layers and size, when using SNS in combination with other services (_e.g., SQS, S3, etc_).

## Advanced

### Built-in JMESPath functions

You can use our built-in JMESPath functions within your envelope expression. They handle deserialization for common data formats found in AWS Lambda event sources such as JSON strings, base64, and uncompress gzip data.

#### powertools_json function

Use `powertools_json` function to decode any JSON string anywhere a JMESPath expression is allowed.

> **Idempotency scenario**

This sample will deserialize the JSON string within the `body` key before [Idempotency](./idempotency.md){target="_blank"} processes it.

=== "Idempotency utility: WithEventKeyJmesPath"

    ```csharp hl_lines="4"
    Idempotency.Configure(builder =>
            builder
                .WithOptions(optionsBuilder =>
                    optionsBuilder.WithEventKeyJmesPath("powertools_json(Body).[\"user_id\", \"product_id\"]"))
                .UseDynamoDb("idempotency_table"));
    ```

=== "Payload"

    ```json hl_lines="28"
    {
      "version": "2.0",
      "routeKey": "ANY /createpayment",
      "rawPath": "/createpayment",
      "rawQueryString": "",
      "headers": {
        "Header1": "value1",
        "Header2": "value2"
      },
      "requestContext": {
        "accountId": "123456789012",
        "apiId": "api-id",
        "domainName": "id.execute-api.us-east-1.amazonaws.com",
        "domainPrefix": "id",
        "http": {
          "method": "POST",
          "path": "/createpayment",
          "protocol": "HTTP/1.1",
          "sourceIp": "ip",
          "userAgent": "agent"
        },
        "requestId": "id",
        "routeKey": "ANY /createpayment",
        "stage": "$default",
        "time": "10/Feb/2021:13:40:43 +0000",
        "timeEpoch": 1612964443723
      },
      "body": "{\"user_id\":\"xyz\",\"product_id\":\"123456789\"}",
      "isBase64Encoded": false
    }
    ```

#### powertools_base64 function

Use `powertools_base64` function to decode any base64 data.

This sample will decode the base64 value within the `data` key, and deserialize the JSON string before validation.

=== "Function"

    ```csharp
    var transformer = JsonTransformer.Parse("powertools_base64(body).customerId");
    using var result = transformer.Transform(doc.RootElement);
    
    Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
    ```

=== "Payload"

    ```json
     {
      "body": "eyJjdXN0b21lcklkIjoiZGQ0NjQ5ZTYtMjQ4NC00OTkzLWFjYjgtMGY5MTIzMTAzMzk0In0=",
      "deeply_nested": [
        {
          "some_data": [
            1,
            2,
            3
          ]
        }
      ]
    }
    ```

#### powertools_base64_gzip function

Use `powertools_base64_gzip` function to decompress and decode base64 data.

This sample will decompress and decode base64 data from Cloudwatch Logs, then use JMESPath pipeline expression to pass the result for decoding its JSON string.

=== "Function"

    ```csharp
    var transformer = JsonTransformer.Parse("powertools_base64_gzip(body).customerId");
    using var result = transformer.Transform(doc.RootElement);
    
    Logger.LogInformation(result.RootElement.GetRawText()); // "dd4649e6-2484-4993-acb8-0f9123103394"
    ```

=== "Payload"

    ```json
    {
      "body": "H4sIAAAAAAAAA6tWSi4tLsnPTS3yTFGyUkpJMTEzsUw10zUysTDRNbG0NNZNTE6y0DVIszQ0MjY0MDa2NFGqBQCMzDWgNQAAAA==",
      "deeply_nested": [
        {
          "some_data": [
            1,
            2,
            3
          ]
        }
      ]
    }
    ```
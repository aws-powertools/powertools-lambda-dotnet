# Powertools JMESPath support

JMESPath is a query language for JSON used by AWS CLI, AWS Python SDK, and Powertools for AWS Lambda.

With built-in JMESPath functions to easily deserialize common encoded JSON payloads in Lambda functions.

## Key features

- Deserialize JSON from JSON strings, base64, and compressed data
- Use JMESPath to extract and combine data recursively
- Provides commonly used JMESPath expression with popular event sources

JMESPath allows you to transform a JsonDocument into another JsonDocument.

For example, consider the JSON data

```csharp

string jsonString = """
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
""";

using JsonDocument doc = JsonDocument.Parse(jsonString);

string expr = "powertools_json(body).customerId";
//also works for fetching and flattening deeply nested data
// string expr = "deeply_nested[*].some_data[]";

JsonDocument result = JsonTransformer.Transform(doc.RootElement, expr);



```

It produces the result
```json
"dd4649e6-2484-4993-acb8-0f9123103394"
```
 You can find more examples [here](../../tests/AWS.Lambda.Powertools.JMESPath.Tests/JmesPathExamples.cs)
 
## Built-in envelopes

We provide built-in envelopes for popular AWS Lambda event sources to easily decode and/or deserialize JSON objects.

| Envelop             | JMESPath expression                                                         |
|---------------------|-----------------------------------------------------------------------------|
| API_GATEWAY_HTTP    | powertools_json(body)                                                       |
| API_GATEWAY_REST    | powertools_json(body)                                                       |
| CLOUDWATCH_LOGS     | awslogs.powertools_base64_gzip(data) &#124; powertools_json(@).logEvents[*] |
| KINESIS_DATA_STREAM | Records[*].kinesis.powertools_json(powertools_base64(data))                 |
| SNS                 | Records[*].Sns.Message &#124; powertools_json(@)                            |
| SQS                 | Records[*].powertools_json(body)                                            |

More examples of events can be found [here](../../tests/AWS.Lambda.Powertools.JMESPath.Tests/test_files)

## Built-in JMESPath functions
You can use our built-in JMESPath functions within your envelope expression. They handle deserialization for common data formats found in AWS Lambda event sources such as JSON strings, base64, and uncompress gzip data.

### powertools_json function
Use powertools_json function to decode any JSON string anywhere a JMESPath expression is allowed.

### powertools_base64 function
Use powertools_base64 function to decode any base64 data.

### powertools_base64_gzip function
Use powertools_base64_gzip function to decompress and decode base64 data.

## Credit
We took heavy inspiration in the https://github.com/danielaparker/JsonCons.Net repository.
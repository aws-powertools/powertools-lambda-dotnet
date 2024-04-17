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
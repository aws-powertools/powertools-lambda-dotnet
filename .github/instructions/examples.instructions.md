---
applyTo: "examples/**/*.cs"
---

# Example Code Standards for Powertools

## Purpose

Examples should demonstrate best practices and be production-ready patterns that developers can copy.

## Lambda Handler Patterns

- Keep handlers focused and lightweight
- Use proper dependency injection setup
- Show realistic error handling
- Demonstrate proper logging and tracing

```csharp
[Logging(LogEvent = true)]
[Metrics(CaptureColdStart = true)]
[Tracing(CaptureMode = TracingCaptureMode.ResponseAndError)]
public async Task<APIGatewayProxyResponse> FunctionHandler(
    APIGatewayProxyRequest request, 
    ILambdaContext context)
{
    try
    {
        // Handler implementation
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(response)
        };
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error processing request");
        throw;
    }
}
```

## Configuration Examples

- Show environment variable usage
- Demonstrate proper configuration validation
- Include both simple and advanced configurations
- Show integration with AWS services

```csharp
// Good configuration example
public class Function
{
    private readonly IParametersProvider _parametersProvider;
    
    public Function()
    {
        _parametersProvider = ParametersManager.GetProvider(ParameterStoreProvider.Get());
    }
    
    public async Task<string> FunctionHandler(string input, ILambdaContext context)
    {
        var config = await _parametersProvider.GetAsync<AppConfig>("/myapp/config");
        // Use configuration
    }
}
```

## Error Handling Examples

- Show proper exception handling patterns
- Demonstrate graceful degradation
- Include retry logic where appropriate
- Show how to handle AWS service exceptions

```csharp
// Good error handling in examples
try
{
    var result = await ProcessRequest(request);
    return CreateSuccessResponse(result);
}
catch (ValidationException ex)
{
    Logger.LogWarning("Validation failed: {Error}", ex.Message);
    return CreateErrorResponse(400, "Invalid request");
}
catch (Exception ex)
{
    Logger.LogError(ex, "Unexpected error processing request");
    return CreateErrorResponse(500, "Internal server error");
}
```

## Testing Examples

- Include unit tests for example functions
- Show how to test with Powertools utilities
- Demonstrate mocking of AWS services
- Include integration test patterns

## Documentation in Examples

- Include clear README files
- Add inline comments explaining Powertools usage
- Show deployment instructions
- Include troubleshooting tips

## Realistic Data Usage

- Use realistic but non-sensitive example data
- Show proper data validation patterns
- Demonstrate serialization/deserialization
- Include edge case handling

## Performance Best Practices

- Show proper async/await usage
- Demonstrate connection reuse patterns
- Include caching examples where appropriate
- Show memory-efficient patterns
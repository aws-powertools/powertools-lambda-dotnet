---
applyTo: "**/*.cs"
---

# C# Development Standards for Powertools

## Naming Conventions

- Use PascalCase for classes, methods, properties, and public fields
- Use camelCase with underscore prefix for private fields (`_loggerInstance`)
- Use PascalCase for constants and static readonly fields
- Use descriptive names that reveal intent

```csharp
// Avoid
private static ILogger l;
public void DoStuff() { }

// Prefer  
private static ILogger _loggerInstance;
public void ProcessLambdaEvent() { }
```

## Type Safety and Modern C#

- Use nullable reference types consistently
- Prefer target-typed new expressions where clear
- Use file-scoped namespaces for new files
- Use pattern matching and switch expressions appropriately

```csharp
// Prefer file-scoped namespaces
namespace AWS.Lambda.Powertools.Logging;

// Use nullable reference types
public string? OptionalProperty { get; set; }

// Target-typed new
private static readonly object Lock = new();
```

## Async/Await Patterns

- Always use ConfigureAwait(false) in library code
- Prefer async methods over sync wrappers
- Use proper cancellation token support
- Handle async exceptions appropriately

```csharp
// Library code pattern
public async Task<string> GetParameterAsync(string name, CancellationToken cancellationToken = default)
{
    var response = await _client.GetParameterAsync(request, cancellationToken).ConfigureAwait(false);
    return response.Parameter.Value;
}
```

## Error Handling

- Use specific exception types, not generic Exception
- Provide meaningful error messages with context
- Include parameter names in ArgumentException messages
- Use custom exceptions for domain-specific errors

```csharp
// Good error handling
if (string.IsNullOrEmpty(parameterName))
    throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));

// Custom exceptions
public class PowertoolsConfigurationException : Exception
{
    public PowertoolsConfigurationException(string message) : base(message) { }
}
```

## Thread Safety Patterns

- Use proper locking for shared state
- Prefer immutable objects where possible
- Use concurrent collections for shared data
- Document thread safety guarantees

```csharp
// Thread-safe singleton pattern
private static readonly object Lock = new();
private static ILogger? _instance;

public static ILogger Instance
{
    get
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                _instance ??= CreateLogger();
            }
        }
        return _instance;
    }
}
```

## AWS SDK Usage

- Always dispose AWS clients properly
- Use dependency injection for testability
- Handle AWS service exceptions specifically
- Use proper retry and timeout configurations

```csharp
// Proper AWS client usage
public class ParameterProvider : IDisposable
{
    private readonly IAmazonSystemsManagement _client;
    
    public ParameterProvider(IAmazonSystemsManagement client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }
    
    public void Dispose()
    {
        _client?.Dispose();
    }
}
```

## Documentation Requirements

- All public APIs must have XML documentation
- Include parameter descriptions and return value info
- Document exceptions that can be thrown
- Provide usage examples for complex APIs

```csharp
/// <summary>
/// Retrieves a parameter value from AWS Systems Manager Parameter Store.
/// </summary>
/// <param name="name">The parameter name to retrieve</param>
/// <param name="decrypt">Whether to decrypt SecureString parameters</param>
/// <returns>The parameter value</returns>
/// <exception cref="ArgumentException">Thrown when parameter name is invalid</exception>
/// <exception cref="ParameterNotFoundException">Thrown when parameter doesn't exist</exception>
public async Task<string> GetParameterAsync(string name, bool decrypt = false)
```
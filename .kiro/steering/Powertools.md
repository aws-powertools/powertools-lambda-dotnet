---
inclusion: always
---

# Powertools for AWS Lambda (.NET) - AI Guidance

## Critical Rules

- **DO NOT upgrade AspectInjector** beyond 2.8.1 (known issue #220)
- All code must be **Native AOT compatible** (IsTrimmable, IsAotCompatible)
- All utilities must be **thread-safe** for concurrent Lambda execution
- Never commit secrets; use placeholders: `<name>`, `<email>`, `<address>`

## Build & Test (from `./libraries`)

```bash
dotnet restore
dotnet build --configuration Release --no-restore /tl
dotnet test --no-restore --filter "Category!=E2E"
```

## Code Style

### Naming
- Classes/Methods/Properties/Constants: `PascalCase`
- Fields: `_camelCase` (underscore prefix)
- Parameters: `camelCase`

### File Organization
- Public APIs at namespace root
- Implementation details in `Internal/` folders
- Use partial classes for large utilities (e.g., `Logger.cs`, `Logger.Scope.cs`)
- File-scoped namespaces required

### Using Statement Order
1. System namespaces
2. AWS/Amazon namespaces
3. Microsoft namespaces
4. Local project namespaces

### Modern C# Features (Required)
```csharp
namespace AWS.Lambda.Powertools.Logging;  // File-scoped

public string? OptionalProperty { get; set; }  // Nullable reference types
private static readonly object Lock = new();   // Target-typed new
```

## Architecture Patterns

### AOP via AspectInjector
```csharp
[Logging(LogEvent = true)]
public void Handler(string input, ILambdaContext context) { }
```

### Thread-Safe Singletons
```csharp
private static readonly object Lock = new();
private static ILogger? _instance;
```

### Internal API Testing
```csharp
[assembly: InternalsVisibleTo("AWS.Lambda.Powertools.{Feature}.Tests")]
```

## Testing

- **Framework**: xUnit only
- **Mocking**: NSubstitute
- **Pattern**: `MethodName_Should_ExpectedBehavior_When_Condition`
- **E2E tests**: Mark with `[Trait("Category", "E2E")]`

## Namespace Convention

- Public: `AWS.Lambda.Powertools.{Feature}`
- Internal: `AWS.Lambda.Powertools.{Feature}.Internal`
- Tests: `AWS.Lambda.Powertools.{Feature}.Tests`

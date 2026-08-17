---
name: aot-compatibility
description: Guide for ensuring Native AOT compatibility in Powertools code. Use this when asked to fix AOT warnings, add source generators, handle trimming issues, or make code AOT-compatible.
---

# Native AOT Compatibility

This skill guides you through making Powertools code compatible with .NET Native AOT compilation.

## Why AOT Matters

Powertools targets Lambda functions where:
- Cold start time is critical
- Native AOT reduces cold starts by 50-80%
- All utilities MUST be AOT-compatible

## AOT Configuration

The project has AOT enabled in `Directory.Build.props`:

```xml
<PropertyGroup Condition="$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net8.0'))">
    <IsTrimmable>true</IsTrimmable>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

## Common AOT Violations and Fixes

### 1. Reflection-Based Instantiation

```csharp
// ❌ VIOLATION - IL2026, IL2067
var instance = Activator.CreateInstance(type);
var obj = Activator.CreateInstance<T>();

// ✅ FIX - Use factory pattern
public interface IHandlerFactory
{
    IHandler Create();
}

public class LoggingHandlerFactory : IHandlerFactory
{
    public IHandler Create() => new LoggingHandler();
}
```

### 2. Type.GetType() Calls

```csharp
// ❌ VIOLATION - IL2057
var type = Type.GetType(typeName);

// ✅ FIX - Use known types or switch expressions
var handler = handlerName switch
{
    "logging" => new LoggingHandler(),
    "metrics" => new MetricsHandler(),
    _ => throw new NotSupportedException($"Unknown handler: {handlerName}")
};
```

### 3. Unbounded JSON Serialization

```csharp
// ❌ VIOLATION - IL2026
var json = JsonSerializer.Serialize(obj);
var result = JsonSerializer.Deserialize<T>(json);

// ✅ FIX - Use source-generated context
[JsonSerializable(typeof(LogEntry))]
[JsonSerializable(typeof(MetricData))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class PowertoolsSerializerContext : JsonSerializerContext { }

// Usage
var json = JsonSerializer.Serialize(entry, PowertoolsSerializerContext.Default.LogEntry);
var result = JsonSerializer.Deserialize(json, PowertoolsSerializerContext.Default.LogEntry);
```

### 4. MakeGenericType/MakeGenericMethod

```csharp
// ❌ VIOLATION - IL2060
var genericType = typeof(Handler<>).MakeGenericType(eventType);

// ✅ FIX - Use static generic methods or pre-defined types
public static class HandlerFactory
{
    public static IHandler<SQSEvent> CreateSqsHandler() => new SqsHandler();
    public static IHandler<KinesisEvent> CreateKinesisHandler() => new KinesisHandler();
}
```

### 5. Dynamic Code Generation

```csharp
// ❌ VIOLATION
var lambda = Expression.Lambda<Func<T>>(body).Compile();

// ✅ FIX - Use delegates or static methods
Func<T> factory = () => new ConcreteType();
```

## Trim Annotations

When reflection is unavoidable, use trim annotations:

```csharp
// Preserve all public properties for serialization
public T Deserialize<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicProperties)] T>(string json)
{
    return JsonSerializer.Deserialize<T>(json)!;
}

// Preserve parameterless constructor
public void Register<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()
{
    // Registration logic
}

// Suppress warning when you've verified safety
[UnconditionalSuppressMessage("Trimming", "IL2026", 
    Justification = "Type is always included via JsonSerializable attribute")]
public void ProcessKnownType() { }
```

## Creating Source Generators

For complex scenarios, create source generators:

```csharp
// Generator that creates type metadata at compile time
[Generator]
public class PowertoolsSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }
    
    public void Execute(GeneratorExecutionContext context)
    {
        // Generate code at compile time instead of runtime reflection
    }
}
```

## Testing AOT Compatibility

### 1. Build with Warnings as Errors

```bash
cd libraries
dotnet build --configuration Release /p:TreatWarningsAsErrors=true
```

### 2. Check for Specific Warnings

```bash
dotnet build 2>&1 | grep -E "IL2\d{3}|IL3\d{3}"
```

### 3. Publish as AOT

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishAot=true
```

### 4. Run AOT E2E Tests

```bash
cd libraries/tests/e2e/infra-aot
cdk deploy CoreStack --require-approval never
cd ../functions/core
dotnet test --filter "Category=E2E"
```

## Common Warning Codes

| Code | Description | Common Fix |
|------|-------------|------------|
| IL2026 | Requires unreferenced code | Add `[JsonSerializable]` or suppress |
| IL2057 | Unrecognized `Type.GetType` | Use static type references |
| IL2060 | `MakeGenericType` with unknown type | Use factory pattern |
| IL2067 | Type passed to reflection API | Add `[DynamicallyAccessedMembers]` |
| IL2072 | Value returned from reflection | Add return type annotations |
| IL2075 | `GetType().GetMethod()` | Use static method references |
| IL3050 | Requires dynamic code | Avoid `Expression.Compile()` |

## Key Files

- `libraries/src/Directory.Build.props` - AOT configuration
- `libraries/src/AWS.Lambda.Powertools.Logging/Serializers/` - Source-generated contexts
- `libraries/tests/e2e/infra-aot/` - AOT test infrastructure

## Verification Checklist

Before submitting AOT-related changes:

- [ ] No new IL2xxx or IL3xxx warnings in Release build
- [ ] All JSON serialization uses source-generated contexts
- [ ] No `Activator.CreateInstance()` or `Type.GetType()`
- [ ] Reflection APIs have `[DynamicallyAccessedMembers]` annotations
- [ ] AOT E2E tests pass (if modifying core utilities)

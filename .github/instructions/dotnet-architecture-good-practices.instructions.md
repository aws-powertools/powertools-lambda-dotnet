---
applyTo: "**/*.cs,**/*.csproj"
---

# .NET Architecture & Design Guidelines for Powertools

You are an AI assistant specialized in SOLID principles, clean architecture, and .NET best practices for library development. Follow these guidelines for building robust, maintainable utilities.

## Core Principles

### SOLID Principles

- **Single Responsibility (SRP)**: Each class has one reason to change. Split large utilities into partial classes by feature (e.g., `Logger.cs`, `Logger.Scope.cs`, `Logger.Sampling.cs`).
- **Open/Closed (OCP)**: Extend behavior through attributes and handlers, not by modifying existing code. See `MethodAspectAttribute` + `IMethodAspectHandler` pattern.
- **Liskov Substitution (LSP)**: All aspect handlers must be interchangeable via `IMethodAspectHandler` interface.
- **Interface Segregation (ISP)**: Small, focused interfaces. Avoid forcing implementations to depend on unused methods.
- **Dependency Inversion (DIP)**: Depend on abstractions (`ILogger`, `IPowertoolsConfigurations`) not concrete implementations.

### Clean Architecture for Libraries

```
┌─────────────────────────────────────────────┐
│  Public API (Attributes, Extensions)        │  ← Users interact here
├─────────────────────────────────────────────┤
│  Core Logic (Handlers, Processors)          │  ← Business rules
├─────────────────────────────────────────────┤
│  Internal/ (Implementation details)         │  ← Hidden from consumers
├─────────────────────────────────────────────┤
│  Infrastructure (AWS SDK, Serialization)    │  ← External dependencies
└─────────────────────────────────────────────┘
```

- **Public API**: Attributes (`[Logging]`, `[Metrics]`), extension methods, configuration builders
- **Core Logic**: Aspect handlers, processors, validators
- **Internal**: Implementation details in `Internal/` folders with `internal` visibility
- **Infrastructure**: AWS SDK clients, JSON serializers, external integrations

## Design Patterns in This Codebase

### Aspect-Oriented Programming (AOP)

Cross-cutting concerns are implemented via AspectInjector:

```csharp
// 1. Define attribute inheriting from MethodAspectAttribute
[AttributeUsage(AttributeTargets.Method)]
public class LoggingAttribute : MethodAspectAttribute
{
    public bool LogEvent { get; set; }
    
    protected override IMethodAspectHandler CreateHandler()
    {
        return new LoggingAspectHandler(/* dependencies */);
    }
}

// 2. Implement handler with lifecycle hooks
internal class LoggingAspectHandler : IMethodAspectHandler
{
    public void OnEntry(AspectEventArgs eventArgs) { }
    public void OnSuccess(AspectEventArgs eventArgs, object result) { }
    public void OnException(AspectEventArgs eventArgs, Exception exception) { }
    public void OnExit(AspectEventArgs eventArgs) { }
}
```

### Thread-Safe Singleton Pattern

All utilities are static singletons shared across Lambda invocations:

```csharp
public static class Logger
{
    private static readonly object Lock = new();
    private static ILogger? _instance;

    public static ILogger Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (Lock)
                {
                    _instance ??= CreateLogger();
                }
            }
            return _instance;
        }
    }
}
```

### Builder Pattern for Configuration

```csharp
public class PowertoolsLoggerBuilder
{
    private LogLevel _logLevel = LogLevel.Information;
    private string? _service;
    
    public PowertoolsLoggerBuilder WithLogLevel(LogLevel level)
    {
        _logLevel = level;
        return this;
    }
    
    public PowertoolsLoggerBuilder WithService(string service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        return this;
    }
    
    public ILogger Build() => new PowertoolsLogger(_logLevel, _service);
}
```

## .NET Best Practices

### Async/Await Patterns

```csharp
// ✅ ALWAYS use ConfigureAwait(false) in library code
public async Task<string> GetParameterAsync(string name, CancellationToken cancellationToken = default)
{
    var response = await _client.GetParameterAsync(request, cancellationToken).ConfigureAwait(false);
    return response.Parameter.Value;
}

// ✅ Support cancellation tokens
public async Task ProcessAsync(CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    await DoWorkAsync(cancellationToken).ConfigureAwait(false);
}

// ❌ AVOID sync-over-async
public string GetParameter(string name)
{
    return GetParameterAsync(name).GetAwaiter().GetResult(); // Deadlock risk!
}
```

### Null Safety and Validation

```csharp
// ✅ Use nullable reference types
public string? OptionalProperty { get; set; }

// ✅ Validate parameters with descriptive messages
public void Configure(string serviceName)
{
    ArgumentNullException.ThrowIfNull(serviceName);
    ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
}

// ✅ Use null-coalescing for defaults
var service = configuration.Service ?? Environment.GetEnvironmentVariable("POWERTOOLS_SERVICE_NAME") ?? "service_undefined";
```

### Exception Handling

```csharp
// ✅ Use specific exception types
public class PowertoolsConfigurationException : Exception
{
    public PowertoolsConfigurationException(string message) : base(message) { }
    public PowertoolsConfigurationException(string message, Exception inner) : base(message, inner) { }
}

// ✅ Include context in error messages
throw new PowertoolsConfigurationException(
    $"Invalid log level '{value}'. Valid values: {string.Join(", ", Enum.GetNames<LogLevel>())}");

// ✅ Graceful degradation for non-critical failures
try
{
    ApplyConfiguration(config);
}
catch (Exception ex)
{
    _logger.LogWarning("Configuration failed, using defaults: {Error}", ex.Message);
    ApplyDefaults();
}
```

### Modern C# Features

```csharp
// ✅ File-scoped namespaces
namespace AWS.Lambda.Powertools.Logging;

// ✅ Target-typed new
private static readonly object Lock = new();
private static readonly Dictionary<string, object> _cache = new();

// ✅ Pattern matching
return eventType switch
{
    BatchEventType.Sqs => ProcessSqsBatch(records),
    BatchEventType.Kinesis => ProcessKinesisBatch(records),
    BatchEventType.DynamoDbStream => ProcessDynamoDbBatch(records),
    _ => throw new ArgumentOutOfRangeException(nameof(eventType))
};

// ✅ Records for immutable data
public record LogEntry(string Message, LogLevel Level, DateTime Timestamp);

// ✅ Collection expressions (C# 12+)
string[] validLevels = ["Debug", "Information", "Warning", "Error"];
```

## AOT Compatibility Requirements

### Avoid Reflection

```csharp
// ❌ AVOID - breaks AOT
var instance = Activator.CreateInstance(type);
var method = type.GetMethod("Process");
var value = JsonSerializer.Deserialize<T>(json); // unbounded generic

// ✅ USE - AOT compatible
var instance = new ConcreteType();
var value = JsonSerializer.Deserialize(json, SourceGeneratedContext.Default.MyType);
```

### Source Generators for Serialization

```csharp
// ✅ Define serialization context
[JsonSerializable(typeof(LogEntry))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class PowertoolsSerializerContext : JsonSerializerContext { }

// ✅ Use in serialization
var json = JsonSerializer.Serialize(entry, PowertoolsSerializerContext.Default.LogEntry);
```

### Trim Annotations

```csharp
// ✅ Annotate when reflection is unavoidable
public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string json)
{
    return JsonSerializer.Deserialize<T>(json)!;
}
```

## Implementation Checklist

Before submitting code, verify:

### Design Quality
- [ ] Single responsibility - each class has one clear purpose
- [ ] Dependencies injected via constructor, not created internally
- [ ] Public API is minimal and intuitive
- [ ] Implementation details are in `Internal/` folders

### Thread Safety
- [ ] Static state uses proper locking
- [ ] No race conditions in singleton initialization
- [ ] Collections are thread-safe or properly synchronized

### AOT Compatibility
- [ ] No `Activator.CreateInstance()` or `Type.GetType()`
- [ ] JSON serialization uses source generators
- [ ] Reflection annotated with `[DynamicallyAccessedMembers]`

### Async Correctness
- [ ] All async methods use `ConfigureAwait(false)`
- [ ] Cancellation tokens propagated through call chain
- [ ] No sync-over-async (`GetAwaiter().GetResult()`)

### Error Handling
- [ ] Specific exception types for domain errors
- [ ] Parameter validation with descriptive messages
- [ ] Graceful degradation for non-critical failures

## Anti-Patterns to Avoid

```csharp
// ❌ Service Locator - hides dependencies
var logger = ServiceLocator.Get<ILogger>();

// ✅ Constructor Injection - explicit dependencies
public class Handler(ILogger logger) { }

// ❌ God Class - too many responsibilities
public class Powertools { /* logging, metrics, tracing, parameters... */ }

// ✅ Focused Classes - single responsibility
public class Logger { }
public class Metrics { }
public class Tracer { }

// ❌ Leaky Abstraction - exposing implementation details
public interface ILogger { AmazonCloudWatchClient Client { get; } }

// ✅ Clean Interface - hide implementation
public interface ILogger { void LogInformation(string message); }

// ❌ Primitive Obsession - using strings for everything
public void Configure(string logLevel, string service, string correlationIdPath)

// ✅ Strong Types - explicit intent
public void Configure(LogLevel level, ServiceName service, CorrelationIdPath path)
```

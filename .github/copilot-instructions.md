# Powertools for AWS Lambda (.NET)

## Architecture Overview

A developer toolkit implementing AWS serverless best practices via **Aspect-Oriented Programming (AOP)**. Uses [AspectInjector](https://github.com/pamidur/aspect-injector) to inject cross-cutting concerns (logging, tracing, metrics) at compile time.

### Core Libraries (`libraries/src/`)
| Library | Purpose |
|---------|---------|
| `Common` | Shared AOP infrastructure (`UniversalWrapperAspect`, `MethodAspectAttribute`) - **embedded into other packages at build** |
| `Logging` | Structured JSON logging with Lambda context enrichment |
| `Metrics` | CloudWatch EMF metrics with cold start capture |
| `Tracing` | AWS X-Ray tracing with segment/subsegment management |
| `Parameters` | SSM Parameter Store / Secrets Manager retrieval with caching |
| `Idempotency` | DynamoDB-backed idempotent operation handling |
| `BatchProcessing` | SQS/Kinesis/DynamoDB stream batch processing with partial failure reporting |

### Common Library Embedding (Important!)
The `Common` library is NOT a NuGet dependency. In Release builds, its source files are compiled directly into each utility package via `Directory.Build.props`:
```xml
<Compile Include="..\AWS.Lambda.Powertools.Common\**\*.cs">
    <Link>Common\%(RecursiveDir)%(Filename)%(Extension)</Link>
</Compile>
```
This means changes to `Common/` affect ALL utilities. Always build in Release mode to verify embedding works.

### How AOP Works Here
Attributes like `[Logging]`, `[Tracing]`, `[Metrics]` inherit from `UniversalWrapperAttribute` → intercepted by `UniversalWrapperAspect` → wraps method execution with before/after/error handling.

```csharp
// Example: LoggingAttribute wraps Lambda handlers
[Logging(LogEvent = true, CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
public async Task<Response> Handler(Request request, ILambdaContext context) { }
```

### Creating New Aspect Attributes
To add a new cross-cutting concern (e.g., `[Caching]`):

1. **Create attribute** inheriting from `MethodAspectAttribute`:
```csharp
[AttributeUsage(AttributeTargets.Method)]
public class CachingAttribute : MethodAspectAttribute
{
    public int TtlSeconds { get; set; } = 300;
    
    protected override IMethodAspectHandler CreateHandler()
    {
        return new CachingAspectHandler(TtlSeconds);
    }
}
```

2. **Implement handler** with `IMethodAspectHandler`:
```csharp
internal class CachingAspectHandler : IMethodAspectHandler
{
    public void OnEntry(AspectEventArgs eventArgs) { /* before method */ }
    public void OnSuccess(AspectEventArgs eventArgs, object result) { /* after success */ }
    public void OnException(AspectEventArgs eventArgs, Exception exception) { /* on error */ }
    public void OnExit(AspectEventArgs eventArgs) { /* always runs */ }
}
```

3. For async support, override `WrapAsync<T>` in your attribute (see `LoggingAttribute.cs` for patterns).

## Build & Test Commands

```bash
cd libraries

# Build all (Release, multi-target net8.0/net10.0)
dotnet build --configuration Release /tl

# Run unit tests (excludes E2E)
dotnet test --filter "Category!=E2E"

# Run specific test project
dotnet test tests/AWS.Lambda.Powertools.Logging.Tests

# Run single test
dotnet test --filter "FullyQualifiedName~LoggingAttributeTests.Should_LogEvent"

# E2E tests (requires AWS credentials + deployed infrastructure)
dotnet test --filter "Category=E2E"
```

### E2E Test Infrastructure
E2E tests require AWS CDK deployment. Located in `libraries/tests/e2e/`:

```bash
# Deploy infrastructure (requires AWS CLI + CDK + credentials)
cd libraries/tests/e2e/infra
cdk deploy --require-approval never

# For AOT tests
cd ../infra-aot
cdk deploy CoreStack --require-approval never --context architecture=arm64

# Run E2E tests
cd ../functions/core
dotnet test

# ALWAYS destroy after testing
cd ../../infra && cdk destroy --force
cd ../infra-aot && cdk destroy --force
```

## Critical Conventions

### AOT Compatibility (MANDATORY)
All code must be Native AOT compatible. Avoid:
- Reflection (`Type.GetType()`, `Activator.CreateInstance()`)
- Dynamic code generation
- Unbounded generic serialization without `[JsonSerializable]`

Use `[DynamicallyAccessedMembers]` and source generators where reflection is unavoidable.

### Thread Safety
All utilities are static singletons shared across Lambda invocations. Use double-checked locking:
```csharp
private static readonly object Lock = new();
private static ILogger? _instance;
// Always lock when initializing shared state
```

### Async Patterns
Always use `ConfigureAwait(false)` in library code to avoid deadlocks:
```csharp
var result = await client.GetAsync(request, cancellationToken).ConfigureAwait(false);
```

### File Organization
- Partial classes for large utilities: `Logger.cs`, `Logger.Scope.cs`, `Logger.Sampling.cs`
- Implementation details in `Internal/` folders with `internal` visibility
- Expose test internals via `[InternalsVisibleTo("AWS.Lambda.Powertools.*.Tests")]`

## Testing Patterns

- **Framework**: xUnit only
- **Mocking**: NSubstitute exclusively
- **Naming**: `MethodName_Should_Behavior_When_Condition`
- **Reset state**: Call `PowertoolsLoggingBuilderExtensions.ResetAllProviders()` in test setup

```csharp
[Fact]
public void LogInformation_Should_IncludeCorrelationId_When_PathConfigured()
{
    // Arrange - Act - Assert pattern
}

[Trait("Category", "E2E")]  // Mark E2E tests
public void Handler_Should_ProcessBatch_When_SQSEvent() { }
```

## Security Requirements

- Never log sensitive data (credentials, PII, secrets)
- Validate all input parameters with `ArgumentNullException`/`ArgumentException`
- Use placeholder values in examples: `<api-key>`, `<secret>`

## File-Specific Instructions

Additional coding standards apply based on file patterns (see `.github/instructions/`):
- `**/*.cs` → [csharp.instructions.md](.github/instructions/csharp.instructions.md) - naming, async patterns, error handling
- `**/*.cs` → [dotnet-architecture-good-practices.instructions.md](.github/instructions/dotnet-architecture-good-practices.instructions.md) - SOLID, clean architecture, design patterns
- `**/*Tests.cs` → [tests.instructions.md](.github/instructions/tests.instructions.md) - test structure, mocking, fixtures
- `examples/**/*.cs` → [examples.instructions.md](.github/instructions/examples.instructions.md) - production-ready patterns

## Key Files Reference

- [Directory.Build.props](libraries/src/Directory.Build.props) - AOT settings, multi-targeting config
- [Directory.Packages.props](libraries/src/Directory.Packages.props) - Central Package Management
- [UniversalWrapperAspect.cs](libraries/src/AWS.Lambda.Powertools.Common/Aspects/UniversalWrapperAspect.cs) - Core AOP interception
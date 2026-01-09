# AGENTS.md - Powertools for AWS Lambda (.NET)

This guide provides essential information for AI coding agents working on the AWS Lambda Powertools for .NET codebase.

## Project Overview

**AWS Lambda Powertools for .NET** is a developer toolkit implementing serverless best practices for .NET Lambda functions. The solution contains 129+ projects targeting .NET 8.0 and .NET 10.0 with full Native AOT support.

## Build Commands

### Core Build Commands
```bash
# Working directory: ./libraries
cd libraries

# Restore dependencies
dotnet restore

# Build all projects (Release configuration)
dotnet build --configuration Release --no-restore /tl

# Build specific project
dotnet build src/AWS.Lambda.Powertools.Logging --configuration Release
```

### Test Commands
```bash
# Run all unit tests (excludes E2E tests)
dotnet test --no-restore --filter "Category!=E2E"

# Run tests with code coverage
dotnet test --no-restore --filter "Category!=E2E" --collect:"XPlat Code Coverage" --results-directory ./codecov

# Run single test project
dotnet test tests/AWS.Lambda.Powertools.Logging.Tests

# Run specific test method
dotnet test tests/AWS.Lambda.Powertools.Logging.Tests --filter "FullyQualifiedName~HandlerTests.TestMethod"

# Run E2E tests (requires AWS infrastructure)
dotnet test --filter "Category=E2E"
```

### Documentation Commands
```bash
# Build documentation (requires Docker)
make build-docs

# Serve docs locally with Docker
make docs-local-docker
```

## Project Structure

### Source Libraries (`libraries/src/`)
- `AWS.Lambda.Powertools.Common` - Core shared functionality
- `AWS.Lambda.Powertools.Logging` - Structured JSON logging
- `AWS.Lambda.Powertools.Metrics` - CloudWatch EMF metrics  
- `AWS.Lambda.Powertools.Tracing` - AWS X-Ray tracing
- `AWS.Lambda.Powertools.Parameters` - Parameter Store/Secrets Manager
- `AWS.Lambda.Powertools.Idempotency` - Idempotent operations
- `AWS.Lambda.Powertools.BatchProcessing` - SQS/Kinesis/DynamoDB batch processing
- `AWS.Lambda.Powertools.EventHandler` - AppSync/Bedrock event handling
- `AWS.Lambda.Powertools.Kafka.*` - Kafka event processing (Avro, JSON, Protobuf)

### Test Organization (`libraries/tests/`)
- Unit tests: `AWS.Lambda.Powertools.*.Tests/`
- E2E tests: `e2e/functions/` with AWS CDK infrastructure
- Concurrency tests: `AWS.Lambda.Powertools.ConcurrencyTests/`

## Code Style Guidelines

### Namespace Conventions
```csharp
// Public APIs
AWS.Lambda.Powertools.{UtilityName}

// Internal implementation details
AWS.Lambda.Powertools.{UtilityName}.Internal
```

### Project Naming
- Libraries: `AWS.Lambda.Powertools.{Feature}`
- Tests: `AWS.Lambda.Powertools.{Feature}.Tests`
- Internal namespaces: Use `Internal/` folders

### File Organization
- Use partial classes for feature-specific functionality (e.g., `Logger.*.cs`)
- Place implementation details in `Internal/` folders
- Dedicated folders for serialization logic (`Serializers/`)

### Import/Using Statements
```csharp
// System namespaces first
using System;
using System.Text.Json;

// AWS/Amazon namespaces
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Common;

// Microsoft namespaces
using Microsoft.Extensions.Logging;

// Local project namespaces last
using AWS.Lambda.Powertools.Logging.Internal;
```

### Naming Conventions
- **Classes**: PascalCase (`PowertoolsLogger`)
- **Methods**: PascalCase (`LogInformation`)
- **Properties**: PascalCase (`LogLevel`)
- **Fields**: camelCase with underscore prefix (`_loggerInstance`)
- **Constants**: PascalCase (`Lock`)
- **Parameters**: camelCase (`loggerFactory`)

### Type Definitions
```csharp
// Use modern C# features
public static partial class Logger  // Partial classes for large utilities

// Nullable reference types enabled by default
public string? OptionalProperty { get; set; }

// Use target-typed new expressions
private static readonly object Lock = new();

// Use file-scoped namespaces
namespace AWS.Lambda.Powertools.Logging;
```

### Error Handling
```csharp
// Custom exceptions for domain-specific errors
public class LogFormatException : Exception
{
    public LogFormatException(string message) : base(message) { }
}

// Parameter validation with descriptive messages
if (loggerFactory == null) 
    throw new ArgumentNullException(nameof(loggerFactory));

// Graceful degradation for configuration issues
try 
{
    // Configuration logic
}
catch (Exception ex)
{
    // Fallback mechanism
    _logger.LogWarning("Configuration failed, using defaults: {Error}", ex.Message);
}
```

### Documentation
```csharp
/// <summary>
/// Class Logger provides structured JSON logging for Lambda functions.
/// </summary>
/// <param name="loggerFactory">The factory to use for creating loggers</param>
/// <returns>Configured logger instance</returns>
public static ILogger Configure(ILoggerFactory loggerFactory)
```

## Testing Patterns

### Test Framework
- **Framework**: xUnit exclusively
- **Mocking**: NSubstitute for test doubles
- **Categories**: Use `[Fact]` for unit tests, `Category` attributes for E2E tests

### Test Structure
```csharp
public class HandlerTests
{
    private readonly ILogger _logger;

    public HandlerTests(ILogger logger)
    {
        _logger = logger;
        // Reset state for isolation
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
    }

    [Fact]
    public void TestMethod_Should_LogCorrectly_When_ValidInput()
    {
        // Arrange
        var expected = "test value";
        
        // Act
        _logger.LogInformation("Test message: {Value}", expected);
        
        // Assert
        // Verification logic
    }
}
```

### E2E Test Categories
```csharp
[Fact]
[Trait("Category", "E2E")]
public void E2ETest_Should_ProcessBatch_When_ValidSQSEvent()
{
    // E2E test implementation
}
```

## Configuration Files

### MSBuild Configuration
- **Directory.Build.props**: Centralized project properties
- **Directory.Packages.props**: Central Package Management (CPM)
- **Multi-targeting**: net8.0 and net10.0
- **AOT Support**: IsTrimmable, EnableTrimAnalyzer, IsAotCompatible enabled

### Key Dependencies
- Amazon.Lambda.Core (2.8.0)
- AspectInjector (2.8.1) - for AOP functionality
- Microsoft.Extensions.* packages for DI/Logging
- xUnit for testing, NSubstitute for mocking

## Architecture Patterns

### SOLID Principles

This codebase follows SOLID principles adapted for library development:

- **Single Responsibility**: Split large utilities into partial classes by feature (e.g., `Logger.cs`, `Logger.Scope.cs`, `Logger.Sampling.cs`)
- **Open/Closed**: Extend behavior through `MethodAspectAttribute` + `IMethodAspectHandler` pattern without modifying existing code
- **Liskov Substitution**: All aspect handlers are interchangeable via `IMethodAspectHandler` interface
- **Interface Segregation**: Small, focused interfaces (`ILogger`, `IPowertoolsConfigurations`)
- **Dependency Inversion**: Depend on abstractions, inject dependencies via constructors

For detailed architecture guidelines, see [dotnet-architecture-good-practices.instructions.md](.github/instructions/dotnet-architecture-good-practices.instructions.md).

### Aspect-Oriented Programming
Uses AspectInjector for cross-cutting concerns like logging, metrics, and tracing:
```csharp
[Logging(LogEvent = true)]
public void TestMethod(string message, ILambdaContext lambdaContext)
{
    // Method implementation
}
```

### Singleton Pattern with Thread Safety
```csharp
private static readonly object Lock = new object();
private static ILogger _loggerInstance;

private static ILogger LoggerInstance 
{
    get
    {
        if (_loggerInstance == null)
        {
            lock (Lock)
            {
                if (_loggerInstance == null)
                {
                    _loggerInstance = Initialize();
                }
            }
        }
        return _loggerInstance;
    }
}
```

### Internal API Exposure
Use `InternalsVisibleTo` for controlled internal API access in tests:
```csharp
[assembly: InternalsVisibleTo("AWS.Lambda.Powertools.Logging.Tests")]
```

## Common Patterns

### Fluent Configuration
```csharp
Logger.Configure(builder => builder
    .SetLogLevel(LogLevel.Information)
    .SetService("MyService")
    .SetVersion("1.0.0"));
```

### Extension Methods
```csharp
public static class PowertoolsLoggerExtensions
{
    public static void AppendKey(this ILogger logger, string key, object value)
    {
        // Implementation
    }
}
```

## Performance Considerations

- **Native AOT**: All code must be AOT-compatible with trimming enabled
- **Multi-targeting**: Support both .NET 8.0 and .NET 10.0
- **Thread Safety**: All utilities must be thread-safe for concurrent Lambda execution
- **Memory Efficiency**: Use object pooling and avoid unnecessary allocations

## Security Guidelines

- Never commit secrets or credentials
- Use placeholder values in examples: `<name>`, `<email>`, `<address>`
- Follow AWS security best practices for Lambda functions
- Validate all input parameters

## CI/CD Integration

The build pipeline runs:
1. `dotnet restore` - Restore dependencies
2. `dotnet build --configuration Release --no-restore /tl` - Build all projects
3. `dotnet test --no-restore --filter "Category!=E2E"` - Run unit tests
4. Code coverage collection and Codecov reporting

Always ensure your changes pass all build steps before submitting PRs.
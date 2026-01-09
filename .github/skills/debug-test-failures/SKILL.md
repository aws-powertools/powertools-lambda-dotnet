---
name: debug-test-failures
description: Guide for debugging failing tests in Powertools. Use this when asked to fix test failures, debug unit tests, understand test output, or troubleshoot test isolation issues.
---

# Debugging Test Failures

This skill guides you through diagnosing and fixing test failures in the Powertools codebase.

## Common Test Failure Patterns

### 1. State Pollution Between Tests

**Symptom**: Tests pass individually but fail when run together.

**Cause**: Powertools utilities are static singletons that persist across tests.

**Fix**: Reset state in test setup:

```csharp
public class LoggingTests : IDisposable
{
    public LoggingTests()
    {
        // Reset ALL shared state before each test
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
        Logger.Reset(); // If available
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
    }

    public void Dispose()
    {
        // Cleanup after test
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
    }
}
```

### 2. Environment Variable Leakage

**Symptom**: Tests fail with unexpected configuration values.

**Cause**: Environment variables set by one test affect another.

**Fix**: Use a test fixture that saves/restores environment:

```csharp
public class EnvironmentFixture : IDisposable
{
    private readonly Dictionary<string, string?> _originalValues = new();
    private readonly string[] _powertoolsVars = 
    {
        "POWERTOOLS_SERVICE_NAME",
        "POWERTOOLS_LOG_LEVEL",
        "POWERTOOLS_LOGGER_CASE",
        "POWERTOOLS_METRICS_NAMESPACE"
    };

    public EnvironmentFixture()
    {
        foreach (var key in _powertoolsVars)
        {
            _originalValues[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    public void Dispose()
    {
        foreach (var (key, value) in _originalValues)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
```

### 3. Async Test Timing Issues

**Symptom**: Tests fail intermittently with timeout or race conditions.

**Cause**: Missing `await`, improper async handling, or timing assumptions.

**Fix**:

```csharp
// ❌ BAD - Fire and forget
[Fact]
public void Test_AsyncOperation()
{
    var task = _handler.ProcessAsync(); // Not awaited!
    Assert.True(true);
}

// ✅ GOOD - Properly awaited
[Fact]
public async Task Test_AsyncOperation()
{
    await _handler.ProcessAsync();
    Assert.True(_handler.WasProcessed);
}

// ✅ GOOD - With timeout
[Fact]
public async Task Test_AsyncOperation_WithTimeout()
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await _handler.ProcessAsync(cts.Token);
}
```

### 4. Mock Setup Issues (NSubstitute)

**Symptom**: Tests fail because mocked method returns null or wrong value.

**Cause**: Mock not configured for the exact arguments used.

**Fix**:

```csharp
// ❌ BAD - Too specific
mockClient.GetParameterAsync(new GetParameterRequest { Name = "exact-name" })
    .Returns(response);

// ✅ GOOD - Use Arg matchers
mockClient.GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>())
    .Returns(response);

// ✅ GOOD - Match specific property
mockClient.GetParameterAsync(
    Arg.Is<GetParameterRequest>(r => r.Name.StartsWith("/myapp/")),
    Arg.Any<CancellationToken>())
    .Returns(response);
```

### 5. Aspect/AOP Test Failures

**Symptom**: Aspect handler methods not being called.

**Cause**: AspectInjector requires specific patterns.

**Fix**: Ensure the method is virtual or the class is properly decorated:

```csharp
public class TestHandler
{
    // ✅ Method must be in a class that can be intercepted
    [Logging]
    public virtual string Process() => "result";
}

// In test
[Fact]
public void Logging_Should_BeInvoked()
{
    var handler = new TestHandler();
    var result = handler.Process();
    
    // Verify logging occurred
}
```

## Debugging Commands

### Run Single Test with Verbose Output

```bash
dotnet test tests/AWS.Lambda.Powertools.Logging.Tests \
    --filter "FullyQualifiedName~TestMethodName" \
    --logger "console;verbosity=detailed"
```

### Run Tests with Code Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults \
    --filter "Category!=E2E"
```

### Debug in VS Code

Add to `.vscode/launch.json`:

```json
{
    "name": "Debug Test",
    "type": "coreclr",
    "request": "launch",
    "program": "dotnet",
    "args": [
        "test",
        "${workspaceFolder}/libraries/tests/AWS.Lambda.Powertools.Logging.Tests",
        "--filter",
        "FullyQualifiedName~YourTestMethod",
        "--no-build"
    ],
    "cwd": "${workspaceFolder}/libraries"
}
```

## Test Output Analysis

### Understanding xUnit Output

```
[FAIL] LoggingTests.LogInformation_Should_WriteJson_When_Called
    Expected: {"level":"Information","message":"test"}
    Actual:   {"level":"Info","message":"test"}
```

**Reading the output**:
1. `[FAIL]` - Test failed
2. `LoggingTests.LogInformation_Should_WriteJson_When_Called` - Test name following naming convention
3. `Expected` vs `Actual` - What assertion failed

### Check for Test Pollution

Run tests in random order to detect pollution:

```bash
dotnet test --logger "console;verbosity=normal" -- xUnit.MethodDisplay=Method
```

## Verification Steps

When fixing a test failure:

1. **Reproduce locally**: Run the exact failing test
2. **Run in isolation**: `dotnet test --filter "FullyQualifiedName~SpecificTest"`
3. **Run with neighbors**: Run the full test class
4. **Run full suite**: `dotnet test --filter "Category!=E2E"`
5. **Check for flakiness**: Run 3-5 times to ensure stability

## Key Test Files

- `libraries/tests/AWS.Lambda.Powertools.Common.Tests/` - Base aspect tests
- `libraries/tests/AWS.Lambda.Powertools.Logging.Tests/` - Logging utility tests
- `libraries/tests/AWS.Lambda.Powertools.ConcurrencyTests/` - Thread safety tests

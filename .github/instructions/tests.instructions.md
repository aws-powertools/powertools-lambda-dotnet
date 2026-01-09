---
applyTo: "**/*Tests.cs"
---

# Test Code Standards for Powertools

## Test Structure and Naming

- Use descriptive test method names following pattern: `MethodName_Should_ExpectedBehavior_When_Condition`
- Organize tests with Arrange, Act, Assert pattern
- Use xUnit `[Fact]` for simple tests, `[Theory]` for parameterized tests
- Group related tests in nested classes when appropriate

```csharp
[Fact]
public void LogInformation_Should_WriteStructuredJson_When_ValidMessageProvided()
{
    // Arrange
    var expectedMessage = "Test message";
    
    // Act
    _logger.LogInformation(expectedMessage);
    
    // Assert
    // Verification logic
}
```

## Test Categories and Traits

- Use `[Trait("Category", "E2E")]` for end-to-end tests
- Use `[Trait("Category", "Unit")]` for unit tests when needed
- Mark slow tests appropriately
- Use descriptive trait names for test organization

```csharp
[Fact]
[Trait("Category", "E2E")]
public void BatchProcessor_Should_ProcessSQSBatch_When_ValidEventsReceived()
{
    // E2E test implementation
}
```

## Mocking with NSubstitute

- Use NSubstitute for all mocking needs
- Mock AWS service clients, not concrete implementations
- Verify important interactions with mocks
- Use clear, descriptive mock setups

```csharp
// Good mocking pattern
var mockClient = Substitute.For<IAmazonSystemsManagement>();
mockClient.GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>())
    .Returns(new GetParameterResponse 
    { 
        Parameter = new Parameter { Value = "test-value" } 
    });
```

## Test Data and Fixtures

- Use realistic test data that represents actual Lambda events
- Create reusable test fixtures for common scenarios
- Avoid hardcoded values - use constants or test data builders
- Clean up resources in test disposal

```csharp
public class LoggerTestFixture : IDisposable
{
    public ILogger Logger { get; }
    
    public LoggerTestFixture()
    {
        // Setup test logger
        PowertoolsLoggingBuilderExtensions.ResetAllProviders();
        Logger = CreateTestLogger();
    }
    
    public void Dispose()
    {
        // Cleanup
    }
}
```

## Async Test Patterns

- Use proper async/await in test methods
- Test both successful and failed async operations
- Use appropriate timeouts for async tests
- Test cancellation scenarios where relevant

```csharp
[Fact]
public async Task GetParameterAsync_Should_ReturnValue_When_ParameterExists()
{
    // Arrange
    var parameterName = "test-parameter";
    
    // Act
    var result = await _provider.GetParameterAsync(parameterName);
    
    // Assert
    Assert.NotNull(result);
}
```

## Thread Safety Testing

- Test concurrent access scenarios for static utilities
- Use Task.Run for parallel execution tests
- Verify thread isolation for context-dependent features
- Test async context preservation

```csharp
[Fact]
public async Task Logger_Should_MaintainIsolation_When_ConcurrentAccess()
{
    // Arrange
    var tasks = new List<Task>();
    
    // Act - Create multiple concurrent operations
    for (int i = 0; i < 10; i++)
    {
        tasks.Add(Task.Run(() => _logger.LogInformation($"Message {i}")));
    }
    
    await Task.WhenAll(tasks);
    
    // Assert - Verify isolation
}
```

## AWS Service Testing

- Mock AWS service responses realistically
- Test AWS service exception scenarios
- Verify proper AWS client configuration
- Test retry and timeout behaviors

```csharp
[Fact]
public async Task GetParameter_Should_ThrowParameterNotFoundException_When_ParameterNotFound()
{
    // Arrange
    _mockClient.GetParameterAsync(Arg.Any<GetParameterRequest>())
        .ThrowsAsync(new ParameterNotFoundException("Parameter not found"));
    
    // Act & Assert
    await Assert.ThrowsAsync<ParameterNotFoundException>(
        () => _provider.GetParameterAsync("non-existent"));
}
```

## Test Performance Considerations

- Keep unit tests fast (under 100ms each)
- Use TestOutputHelper for debugging output
- Avoid file system operations in unit tests
- Mock time-dependent operations

## Test Documentation

- Add comments for complex test scenarios
- Document test assumptions and prerequisites
- Explain non-obvious test data choices
- Include references to related issues or requirements
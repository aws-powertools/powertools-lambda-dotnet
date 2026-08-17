---
name: create-aspect-attribute
description: Guide for creating new AOP aspect attributes in Powertools. Use this when asked to add a new cross-cutting concern, create a new attribute like [Logging], [Metrics], [Tracing], or implement aspect-oriented functionality.
---

# Creating New Aspect Attributes

This skill guides you through creating new AOP (Aspect-Oriented Programming) attributes for Powertools using the AspectInjector library.

## Overview

Powertools uses AspectInjector to implement cross-cutting concerns. The pattern involves:
1. An **Attribute** (inherits from `MethodAspectAttribute`) - decorates user methods
2. A **Handler** (implements `IMethodAspectHandler`) - contains the before/after logic
3. The **UniversalWrapperAspect** intercepts decorated methods at compile time

## Step-by-Step Process

### Step 1: Create the Attribute Class

Create a new file `{FeatureName}Attribute.cs` in the utility's root namespace:

```csharp
using System;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.{FeatureName};

/// <summary>
/// Attribute that provides {feature description} for Lambda handlers.
/// </summary>
/// <example>
/// <code>
/// [{FeatureName}(PropertyName = value)]
/// public async Task&lt;Response&gt; Handler(Request request, ILambdaContext context)
/// {
///     // Handler implementation
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class {FeatureName}Attribute : MethodAspectAttribute
{
    /// <summary>
    /// Configuration property description.
    /// </summary>
    public string ConfigProperty { get; set; } = "default";

    /// <summary>
    /// Creates the aspect handler for this attribute.
    /// </summary>
    protected override IMethodAspectHandler CreateHandler()
    {
        return new {FeatureName}AspectHandler(ConfigProperty);
    }
}
```

### Step 2: Create the Aspect Handler

Create `Internal/{FeatureName}AspectHandler.cs`:

```csharp
using System;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.{FeatureName}.Internal;

/// <summary>
/// Handles the aspect lifecycle for {FeatureName} attribute.
/// </summary>
internal class {FeatureName}AspectHandler : IMethodAspectHandler
{
    private readonly string _config;

    public {FeatureName}AspectHandler(string config)
    {
        _config = config;
    }

    /// <summary>
    /// Called before the decorated method executes.
    /// </summary>
    public void OnEntry(AspectEventArgs eventArgs)
    {
        // Access method info: eventArgs.Method, eventArgs.Args, eventArgs.Instance
        // Initialize resources, start timers, capture context, etc.
    }

    /// <summary>
    /// Called after successful method execution.
    /// </summary>
    public void OnSuccess(AspectEventArgs eventArgs, object result)
    {
        // Access the return value via 'result'
        // Log success, record metrics, complete traces, etc.
    }

    /// <summary>
    /// Called when the method throws an exception.
    /// </summary>
    public void OnException(AspectEventArgs eventArgs, Exception exception)
    {
        // Handle or log the exception
        // Record error metrics, add trace annotations, etc.
        // NOTE: Exception is NOT swallowed - it will still propagate
    }

    /// <summary>
    /// Called after method execution (success or failure).
    /// </summary>
    public void OnExit(AspectEventArgs eventArgs)
    {
        // Cleanup resources, flush buffers, close connections, etc.
    }
}
```

### Step 3: For Async Method Support

If you need to wrap the entire async execution (not just entry/exit), override `WrapAsync<T>` in your attribute:

```csharp
protected internal override async Task<T> WrapAsync<T>(
    Func<object[], Task<T>> target, 
    object[] args, 
    AspectEventArgs eventArgs)
{
    var handler = CreateHandler();
    
    try
    {
        handler.OnEntry(eventArgs);
        var result = await target(args).ConfigureAwait(false);
        handler.OnSuccess(eventArgs, result);
        return result;
    }
    catch (Exception ex)
    {
        handler.OnException(eventArgs, ex);
        throw;
    }
    finally
    {
        handler.OnExit(eventArgs);
    }
}
```

### Step 4: Add InternalsVisibleTo

In `InternalsVisibleTo.cs`:

```csharp
[assembly: InternalsVisibleTo("AWS.Lambda.Powertools.{FeatureName}.Tests")]
```

### Step 5: Update Project File

Ensure `.csproj` includes Common library embedding:

```xml
<PropertyGroup>
    <IncludeCommonFiles>true</IncludeCommonFiles>
</PropertyGroup>

<ItemGroup>
    <ProjectReference Include="..\AWS.Lambda.Powertools.Common\AWS.Lambda.Powertools.Common.csproj" />
</ItemGroup>
```

## Key Files to Reference

- `libraries/src/AWS.Lambda.Powertools.Logging/LoggingAttribute.cs` - Full example with async support
- `libraries/src/AWS.Lambda.Powertools.Logging/Internal/LoggingAspect.cs` - Handler implementation
- `libraries/src/AWS.Lambda.Powertools.Common/Aspects/` - Base classes and interfaces

## Testing the Attribute

Create tests that verify:
1. Handler is created with correct configuration
2. `OnEntry` is called before method execution
3. `OnSuccess` is called with return value on success
4. `OnException` is called when method throws
5. `OnExit` is always called (cleanup)

```csharp
[Fact]
public void {FeatureName}Attribute_Should_InvokeOnEntry_When_MethodCalled()
{
    // Arrange
    var handler = new TestHandler();
    
    // Act
    handler.DecoratedMethod();
    
    // Assert - verify OnEntry was called
}
```

## Common Pitfalls

1. **Thread Safety**: Handlers may be called concurrently - avoid mutable shared state
2. **AOT Compatibility**: No reflection in handlers - use source generators if needed
3. **ConfigureAwait(false)**: Always use in async code within handlers
4. **Exception Handling**: Don't swallow exceptions in `OnException` unless intentional

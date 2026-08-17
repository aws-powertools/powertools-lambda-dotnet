---
name: add-new-utility
description: Guide for creating a new Powertools utility package from scratch. Use this when asked to add a new utility, create a new Powertools feature, or scaffold a new library like Logging, Metrics, or Tracing.
---

# Adding a New Powertools Utility

This skill guides you through creating a complete new utility package for Powertools, following established patterns.

## Overview

A Powertools utility typically includes:
- Source library (`libraries/src/AWS.Lambda.Powertools.{UtilityName}/`)
- Unit tests (`libraries/tests/AWS.Lambda.Powertools.{UtilityName}.Tests/`)
- Documentation (`docs/utilities/{utility-name}.md`)
- Examples (`examples/{UtilityName}/`)

## Step 1: Create the Project Structure

### 1.1 Create Source Project

```bash
cd libraries/src
mkdir AWS.Lambda.Powertools.{UtilityName}
cd AWS.Lambda.Powertools.{UtilityName}
```

### 1.2 Create Project File

Create `AWS.Lambda.Powertools.{UtilityName}.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <Description>Powertools for AWS Lambda (.NET) - {UtilityName} utility</Description>
        <PackageTags>AWS;Amazon;Lambda;Powertools;{UtilityName}</PackageTags>
        <IncludeCommonFiles>true</IncludeCommonFiles>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\AWS.Lambda.Powertools.Common\AWS.Lambda.Powertools.Common.csproj" />
    </ItemGroup>

    <!-- Add AWS SDK dependencies as needed -->
    <ItemGroup>
        <PackageReference Include="Amazon.Lambda.Core" />
        <PackageReference Include="AWSSDK.{ServiceName}" Condition="'$(TargetFramework)' == 'net8.0'" />
    </ItemGroup>

</Project>
```

**Important**: `IncludeCommonFiles=true` embeds the Common library at build time.

### 1.3 Create Directory Structure

```
AWS.Lambda.Powertools.{UtilityName}/
├── AWS.Lambda.Powertools.{UtilityName}.csproj
├── README.md
├── InternalsVisibleTo.cs
├── {UtilityName}Attribute.cs          # Main attribute (if AOP-based)
├── {UtilityName}.cs                   # Static entry point
├── {UtilityName}Options.cs            # Configuration options
├── I{UtilityName}Provider.cs          # Main interface
├── Internal/
│   ├── {UtilityName}Handler.cs        # Aspect handler
│   └── {UtilityName}Provider.cs       # Default implementation
└── Serializers/
    └── {UtilityName}SerializerContext.cs  # AOT-compatible serialization
```

## Step 2: Implement Core Components

### 2.1 InternalsVisibleTo.cs

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AWS.Lambda.Powertools.{UtilityName}.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // For NSubstitute
```

### 2.2 Configuration Options

```csharp
namespace AWS.Lambda.Powertools.{UtilityName};

/// <summary>
/// Configuration options for {UtilityName}.
/// </summary>
public class {UtilityName}Options
{
    /// <summary>
    /// Service name for identification. 
    /// Defaults to POWERTOOLS_SERVICE_NAME environment variable.
    /// </summary>
    public string? Service { get; set; }

    /// <summary>
    /// Whether to enable the feature. Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    // Add utility-specific options
}
```

### 2.3 Main Interface

```csharp
namespace AWS.Lambda.Powertools.{UtilityName};

/// <summary>
/// Interface for {UtilityName} operations.
/// </summary>
public interface I{UtilityName}Provider
{
    /// <summary>
    /// Primary operation description.
    /// </summary>
    void DoSomething(string input);

    /// <summary>
    /// Async operation with cancellation support.
    /// </summary>
    Task<TResult> DoSomethingAsync<TResult>(
        string input, 
        CancellationToken cancellationToken = default);
}
```

### 2.4 Static Entry Point

```csharp
using AWS.Lambda.Powertools.{UtilityName}.Internal;

namespace AWS.Lambda.Powertools.{UtilityName};

/// <summary>
/// Static entry point for {UtilityName} utility.
/// </summary>
public static class {UtilityName}
{
    private static readonly object Lock = new();
    private static I{UtilityName}Provider? _instance;

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static I{UtilityName}Provider Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (Lock)
                {
                    _instance ??= new {UtilityName}Provider(new {UtilityName}Options());
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Configures the utility with custom options.
    /// </summary>
    public static void Configure({UtilityName}Options options)
    {
        ArgumentNullException.ThrowIfNull(options);
        lock (Lock)
        {
            _instance = new {UtilityName}Provider(options);
        }
    }

    /// <summary>
    /// Resets the utility to default state. Used for testing.
    /// </summary>
    internal static void Reset()
    {
        lock (Lock)
        {
            _instance = null;
        }
    }
}
```

### 2.5 Attribute (for AOP-based utilities)

```csharp
using System;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.{UtilityName}.Internal;

namespace AWS.Lambda.Powertools.{UtilityName};

/// <summary>
/// Attribute that enables {UtilityName} for Lambda handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class {UtilityName}Attribute : MethodAspectAttribute
{
    /// <summary>
    /// Configuration property.
    /// </summary>
    public bool SomeOption { get; set; }

    /// <inheritdoc />
    protected override IMethodAspectHandler CreateHandler()
    {
        return new {UtilityName}Handler({UtilityName}.Instance, SomeOption);
    }
}
```

### 2.6 README.md

```markdown
# AWS.Lambda.Powertools.{UtilityName}

Powertools for AWS Lambda (.NET) {UtilityName} utility.

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.{UtilityName}
```

## Quick Start

```csharp
using AWS.Lambda.Powertools.{UtilityName};

public class Function
{
    [{UtilityName}]
    public async Task<Response> Handler(Request request, ILambdaContext context)
    {
        // Your code here
    }
}
```

## Documentation

See the [full documentation](https://docs.powertools.aws.dev/lambda/dotnet/utilities/{utility-name}/).
```

## Step 3: Create Test Project

### 3.1 Create Test Project

```bash
cd libraries/tests
mkdir AWS.Lambda.Powertools.{UtilityName}.Tests
cd AWS.Lambda.Powertools.{UtilityName}.Tests
```

### 3.2 Test Project File

Create `AWS.Lambda.Powertools.{UtilityName}.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\AWS.Lambda.Powertools.{UtilityName}\AWS.Lambda.Powertools.{UtilityName}.csproj" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="xunit" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="NSubstitute" />
        <PackageReference Include="FluentAssertions" />
    </ItemGroup>

</Project>
```

### 3.3 Test Base Class

```csharp
namespace AWS.Lambda.Powertools.{UtilityName}.Tests;

public abstract class {UtilityName}TestBase : IDisposable
{
    protected {UtilityName}TestBase()
    {
        // Reset state before each test
        {UtilityName}.Reset();
        ClearEnvironmentVariables();
    }

    public void Dispose()
    {
        {UtilityName}.Reset();
        ClearEnvironmentVariables();
    }

    private void ClearEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", null);
        // Clear other relevant env vars
    }
}
```

### 3.4 Sample Tests

```csharp
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.{UtilityName}.Tests;

public class {UtilityName}Tests : {UtilityName}TestBase
{
    [Fact]
    public void Instance_Should_ReturnSameInstance_When_CalledMultipleTimes()
    {
        // Act
        var instance1 = {UtilityName}.Instance;
        var instance2 = {UtilityName}.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Configure_Should_UpdateInstance_When_CalledWithNewOptions()
    {
        // Arrange
        var options = new {UtilityName}Options { Service = "test-service" };

        // Act
        {UtilityName}.Configure(options);

        // Assert
        Assert.NotNull({UtilityName}.Instance);
    }

    [Fact]
    public void Configure_Should_ThrowArgumentNullException_When_OptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => {UtilityName}.Configure(null!));
    }
}
```

## Step 4: Add to Solution

### 4.1 Update Solution File

```bash
cd libraries
dotnet sln add src/AWS.Lambda.Powertools.{UtilityName}/AWS.Lambda.Powertools.{UtilityName}.csproj
dotnet sln add tests/AWS.Lambda.Powertools.{UtilityName}.Tests/AWS.Lambda.Powertools.{UtilityName}.Tests.csproj
```

### 4.2 Verify Build

```bash
cd libraries
dotnet build --configuration Release
dotnet test tests/AWS.Lambda.Powertools.{UtilityName}.Tests
```

## Step 5: Add Documentation

Create `docs/utilities/{utility-name}.md`:

```markdown
---
title: {UtilityName}
description: {Brief description}
---

# {UtilityName}

{Longer description of what this utility does and why.}

## Key Features

- Feature 1
- Feature 2
- Feature 3

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.{UtilityName}
```

## Getting Started

### Basic Usage

```csharp
[{UtilityName}]
public async Task<Response> Handler(Request request, ILambdaContext context)
{
    // Your handler code
}
```

### Configuration

| Parameter | Environment Variable | Default | Description |
|-----------|---------------------|---------|-------------|
| Service | POWERTOOLS_SERVICE_NAME | service_undefined | Service name |

## Advanced Usage

{Add advanced examples}
```

## Step 6: Create Examples

Create example project in `examples/{UtilityName}/`:

```
examples/{UtilityName}/
├── Powertools{UtilityName}Example.sln
├── README.md
├── template.yaml                    # SAM template
├── events/
│   └── event.json                   # Sample event
├── src/
│   └── {UtilityName}Function/
│       ├── {UtilityName}Function.csproj
│       └── Function.cs
└── test/
    └── {UtilityName}Function.Tests/
        └── FunctionTests.cs
```

## Checklist

Before submitting your new utility:

- [ ] Source project builds without warnings
- [ ] All tests pass
- [ ] AOT compatibility verified (no IL2xxx warnings)
- [ ] README.md with installation and quick start
- [ ] XML documentation on all public APIs
- [ ] Documentation page created
- [ ] Example project with SAM template
- [ ] Added to solution file
- [ ] Thread safety verified for singleton pattern
- [ ] Environment variable support documented

## Key Reference Files

- `libraries/src/AWS.Lambda.Powertools.Logging/` - Complete utility example
- `libraries/src/AWS.Lambda.Powertools.Metrics/` - Metrics pattern
- `libraries/src/AWS.Lambda.Powertools.Parameters/` - Non-AOP utility pattern
- `libraries/src/Directory.Build.props` - Shared build configuration

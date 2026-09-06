---
title: Migration Guide v4
slug: migration-guide-v4
description: Migration guide to upgrade to v4 (.NET 10)
---

# Migration Guide to v4

This guide will help you migrate to v4, which targets the .NET 10 LTS runtime.

## Why upgrade

* **.NET 10 is the current LTS release** - Generally available since November 2025 and supported until November 10, 2028.
* **.NET 8 support is ending** - .NET 8 (LTS) reaches end of support on November 10, 2026. Production applications should move to .NET 10 for the extended support window.
* **Performance and cost** - .NET 10 brings JIT and GC improvements plus expanded Native AOT support, which translate directly into faster cold starts and lower memory usage for Lambda functions.

## Breaking Changes

⚠️ Important: Please review these breaking changes before upgrading:

* Requires .NET 10 - The Powertools packages and examples now target `net10.0`.
* AWS Lambda managed runtime is now `dotnet10` - Update your SAM templates and function configuration accordingly.

> The Powertools for AWS Lambda (.NET) libraries multi-target `net8.0` and `net10.0`, so they remain compatible with .NET 8 during your transition. The examples and recommended runtime, however, now use .NET 10.

## Upgrade Steps

### 1. Install the .NET 10 SDK

Download and install the .NET 10 SDK from [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0).

Verify the installation:

```bash
dotnet --list-sdks
```

### 2. Update Target Framework

Update your `.csproj` file to target .NET 10:

**Before (v3):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**After (v4):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

### 3. Update the Lambda Runtime

Update your AWS SAM templates (and any function configuration) to use the `dotnet10` managed runtime:

**Before (v3):**
```yaml
Resources:
  HelloWorldFunction:
    Type: AWS::Serverless::Function
    Properties:
      Runtime: dotnet8
```

**After (v4):**
```yaml
Resources:
  HelloWorldFunction:
    Type: AWS::Serverless::Function
    Properties:
      Runtime: dotnet10
```

### 4. Update Powertools Packages

Update all Powertools package references to v4:

```xml
<ItemGroup>
  <PackageReference Include="AWS.Lambda.Powertools.Logging" Version="4.0.*" />
  <PackageReference Include="AWS.Lambda.Powertools.Metrics" Version="4.0.*" />
  <PackageReference Include="AWS.Lambda.Powertools.Tracing" Version="4.0.*" />
</ItemGroup>
```

### 5. Native AOT considerations

When publishing with Native AOT, the build OS and architecture must match the target platform (Amazon Linux 2023). The AWS tooling (AWS Toolkit for Visual Studio, the `Amazon.Lambda.Tools` .NET global tool, and the SAM CLI) performs a container build using a .NET 10 Amazon Linux 2023 build image when `PublishAot` is set to `true`. Docker is required when packaging .NET Native AOT Lambda functions on non-Amazon Linux 2023 build environments.

### 6. Build and test

Rebuild and run your test suite to confirm the upgrade:

```bash
dotnet build
dotnet test
```

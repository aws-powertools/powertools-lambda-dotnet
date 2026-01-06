---
title: Migration Guide v3
slug: migration-guide-v3 
description: Migration guide to upgrade to v3
---

# Migration Guide to v3

This guide will help you migrate to v3

## Breaking Changes

⚠️ Important: Please review these breaking changes before upgrading:

* Removed support for .NET 6 - Now requires .NET 8 or later
* AWS SDK v3 no longer supported - Migrated to AWS SDK v4  

## Upgrade Steps

### 1. Update Target Framework

Update your `.csproj` file to target .NET 8 or later:

**Before (v2):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**After (v3):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

### 2. Update AWS SDK Packages

Migrate from AWS SDK v3 to v4. Update package references in your `.csproj`:

**Before (v2):**
```xml
<ItemGroup>
  <PackageReference Include="Amazon.Lambda.Core" Version="2.5.0" />
  <PackageReference Include="AWSSDK.Core" Version="3.7.*" />
  <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.7.*" />
  <PackageReference Include="AWSSDK.S3" Version="3.7.*" />
</ItemGroup>
```

**After (v3):**
```xml
<ItemGroup>
  <PackageReference Include="Amazon.Lambda.Core" Version="2.8.0" />
  <PackageReference Include="AWSSDK.Core" Version="4.0.*" />
  <PackageReference Include="AWSSDK.DynamoDBv2" Version="4.0.*" />
  <PackageReference Include="AWSSDK.S3" Version="4.0.*" />
</ItemGroup>
```

### 3. Update Powertools Packages

Update all Powertools package references to v3:

```xml
<ItemGroup>
  <PackageReference Include="AWS.Lambda.Powertools.Logging" Version="3.0.*" />
  <PackageReference Include="AWS.Lambda.Powertools.Metrics" Version="3.0.*" />
  <PackageReference Include="AWS.Lambda.Powertools.Tracing" Version="3.0.*" />
</ItemGroup>
```
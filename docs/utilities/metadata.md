---
title: Lambda Metadata
description: Utility
---

<!-- markdownlint-disable MD013 -->
The Lambda Metadata utility provides access to the Lambda Metadata Endpoint (LMDS), giving you execution environment metadata like Availability Zone ID.

## Key features

* Retrieve Lambda execution environment metadata
* Automatic caching for the sandbox lifetime
* Thread-safe access
* Native AOT compatible

## Installation

```bash
dotnet add package AWS.Lambda.Powertools.Metadata
```

## Getting started

```csharp
--8<-- "docs/snippets/metadata/GettingStarted.cs:getting_started"
```

## Available metadata

| Property              | Type      | Description                                                              |
|-----------------------|-----------|--------------------------------------------------------------------------|
| `AvailabilityZoneId`  | `string?` | The AZ where the function is running (e.g., `use1-az1`), or `null` when unavailable |

## Error handling

```csharp
--8<-- "docs/snippets/metadata/GettingStarted.cs:error_handling"
```

## Refreshing metadata

Metadata remains constant for the Lambda sandbox lifetime. If you need to force a refresh:

```csharp
--8<-- "docs/snippets/metadata/GettingStarted.cs:refresh_metadata"
```

## Thread safety

`LambdaMetadata.AvailabilityZoneId` is thread-safe. You can access it from multiple concurrent invocations without race conditions.

## Use cases

### Multi-AZ routing

```csharp
--8<-- "docs/snippets/metadata/UseCases.cs:multi_az_routing"
```

### Logging

```csharp
--8<-- "docs/snippets/metadata/UseCases.cs:logging_with_metadata"
```

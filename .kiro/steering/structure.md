# Project Structure

```
powertools-lambda-dotnet/
├── libraries/                    # Main solution directory
│   ├── AWS.Lambda.Powertools.sln
│   ├── src/                      # Source libraries
│   │   ├── AWS.Lambda.Powertools.Common/        # Shared core functionality
│   │   ├── AWS.Lambda.Powertools.Logging/       # Structured JSON logging
│   │   ├── AWS.Lambda.Powertools.Metrics/       # CloudWatch EMF metrics
│   │   ├── AWS.Lambda.Powertools.Tracing/       # X-Ray tracing
│   │   ├── AWS.Lambda.Powertools.Parameters/    # Parameter retrieval
│   │   ├── AWS.Lambda.Powertools.Idempotency/   # Idempotent operations
│   │   ├── AWS.Lambda.Powertools.BatchProcessing/  # Batch processing
│   │   ├── AWS.Lambda.Powertools.EventHandler/  # Event handling
│   │   ├── AWS.Lambda.Powertools.Kafka*/        # Kafka serializers
│   │   ├── Directory.Build.props                # Shared build config
│   │   └── Directory.Packages.props             # Central package versions
│   └── tests/                    # Test projects
│       ├── AWS.Lambda.Powertools.*.Tests/       # Unit tests per library
│       ├── AWS.Lambda.Powertools.ConcurrencyTests/
│       ├── e2e/                                 # E2E tests with CDK
│       └── Directory.Build.props
├── examples/                     # Sample applications
│   ├── Logging/
│   ├── Metrics/
│   ├── Tracing/
│   ├── Parameters/
│   ├── Idempotency/
│   ├── BatchProcessing/
│   └── ServerlessApi/
├── docs/                         # MkDocs documentation
│   ├── core/                     # Core utility docs
│   ├── utilities/                # Utility docs
│   └── snippets/                 # Code examples for docs
└── apidocs/                      # API reference (docfx)
```

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Library | `AWS.Lambda.Powertools.{Feature}` | `AWS.Lambda.Powertools.Logging` |
| Tests | `AWS.Lambda.Powertools.{Feature}.Tests` | `AWS.Lambda.Powertools.Logging.Tests` |
| Namespace | `AWS.Lambda.Powertools.{Feature}` | `AWS.Lambda.Powertools.Logging` |
| Internal | `AWS.Lambda.Powertools.{Feature}.Internal` | `AWS.Lambda.Powertools.Logging.Internal` |

## Source File Organization

Within each library:
- Root: Public API classes and interfaces
- `Internal/`: Implementation details (not part of public API)
- `Serializers/`: JSON/serialization logic
- `Exceptions/`: Custom exception types
- Partial classes for large utilities (e.g., `Logger.cs`, `Logger.Scope.cs`, `Logger.Sampling.cs`)

## Key Files

- `InternalsVisibleTo.cs` - Controls test access to internal APIs
- `README.md` - Per-package NuGet readme
- `*.csproj` - Project file with package metadata

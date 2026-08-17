# Technology Stack

## Runtime & Framework
- **.NET 8.0** and **.NET 10.0** (multi-targeting)
- **Native AOT** fully supported (IsTrimmable, EnableTrimAnalyzer, IsAotCompatible)
- **C#** with modern language features (nullable reference types, file-scoped namespaces)

## Build System
- **MSBuild** with Central Package Management (CPM)
- **Directory.Build.props** for centralized project configuration
- **Directory.Packages.props** for version management

## Key Dependencies
- `Amazon.Lambda.Core` (2.8.0) - Lambda runtime
- `AspectInjector` (2.8.1) - AOP for cross-cutting concerns (DO NOT upgrade - known issue #220)
- `Microsoft.Extensions.Logging` - Logging abstractions
- `Microsoft.Extensions.DependencyInjection` - DI support
- AWS SDK packages for service integrations

## Testing
- **xUnit** - Test framework
- **NSubstitute** - Mocking library
- E2E tests use AWS CDK infrastructure

## Documentation
- **MkDocs** with Material theme
- Docker-based build process

## Common Commands

```bash
# All commands run from ./libraries directory

# Restore dependencies
dotnet restore

# Build all projects
dotnet build --configuration Release --no-restore /tl

# Build specific project
dotnet build src/AWS.Lambda.Powertools.Logging --configuration Release

# Run unit tests (excludes E2E)
dotnet test --no-restore --filter "Category!=E2E"

# Run tests with coverage
dotnet test --no-restore --filter "Category!=E2E" --collect:"XPlat Code Coverage" --results-directory ./codecov

# Run specific test project
dotnet test tests/AWS.Lambda.Powertools.Logging.Tests

# Run specific test method
dotnet test tests/AWS.Lambda.Powertools.Logging.Tests --filter "FullyQualifiedName~HandlerTests.TestMethod"

# Build docs (requires Docker)
make build-docs

# Serve docs locally
make docs-local-docker
```

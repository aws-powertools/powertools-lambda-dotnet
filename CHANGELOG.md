<!-- changelog is partially generated, so it doesn't follow headings and required structure, so we disable it. -->
<!-- markdownlint-disable -->

# Changelog

All notable changes to this project will be documented in this file.
See [Conventional Commits](https://conventionalcommits.org) for commit guidelines.


<a name="1.50.2"></a>
## [1.50.2] - 2025-05-20
## Features

* add DynamicallyAccessedMembers attribute to batch processor and handler types
* **JsonTransformer:** add AOT support and configure JSON serializer options

## Maintenance

* **deps:** bump zgosalvez/github-actions-ensure-sha-pinned-actions
* **deps:** bump github/codeql-action from 3.28.17 to 3.28.18
* **deps:** bump codecov/codecov-action from 5.4.2 to 5.4.3
* **deps:** bump aws-actions/configure-aws-credentials
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump aws-actions/configure-aws-credentials

## Pull Requests

* Merge pull request [#891](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/891) from aws-powertools/patch-versio-release
* Merge pull request [#889](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/889) from aws-powertools/dependabot/github_actions/zgosalvez/github-actions-ensure-sha-pinned-actions-3.0.25
* Merge pull request [#888](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/888) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.18
* Merge pull request [#885](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/885) from aws-powertools/dependabot/github_actions/codecov/codecov-action-5.4.3
* Merge pull request [#883](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/883) from aws-powertools/dependabot/github_actions/aws-actions/configure-aws-credentials-4.2.1
* Merge pull request [#880](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/880) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-eb04b60c566a8862be6b553157c16a92fbbfc45d71b7e4e8593526aecca63f52
* Merge pull request [#882](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/882) from aws-powertools/feature/parameters-aot-support
* Merge pull request [#881](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/881) from aws-powertools/feature/batch-aot-support
* Merge pull request [#879](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/879) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-f6c81d538499f5755c8d1486f0abcda50bb631be391890ef823fcba18803114a
* Merge pull request [#876](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/876) from aws-powertools/dependabot/github_actions/aws-actions/configure-aws-credentials-4.2.0


<a name="1.50.1"></a>
## [1.50.1] - 2025-05-06
## Maintenance

* **deps:** bump zgosalvez/github-actions-ensure-sha-pinned-actions
* **deps:** bump github/codeql-action from 3.28.15 to 3.28.17
* **deps:** bump actions/download-artifact from 4.2.1 to 4.3.0
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump actions/setup-python from 5.5.0 to 5.6.0
* **deps:** bump aws-cdk-lib from 2.189.0 to 2.189.1
* **deps:** bump codecov/codecov-action from 5.4.0 to 5.4.2

## Pull Requests

* Merge pull request [#872](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/872) from aws-powertools/chore/update-version
* Merge pull request [#871](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/871) from aws-powertools/fix-workflow
* Merge pull request [#869](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/869) from hjgraca/chore/logger-formatting-update
* Merge pull request [#864](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/864) from aws-powertools/chore/update-docs
* Merge pull request [#861](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/861) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-95f2ff42251979c043d6cb5b1c82e6ae8189e57e02105813dd1ce124021a418b
* Merge pull request [#853](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/853) from aws-powertools/dependabot/npm_and_yarn/aws-cdk-lib-2.189.1
* Merge pull request [#856](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/856) from aws-powertools/dependabot/github_actions/actions/setup-python-5.6.0
* Merge pull request [#852](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/852) from aws-powertools/dependabot/github_actions/codecov/codecov-action-5.4.2
* Merge pull request [#862](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/862) from aws-powertools/dependabot/github_actions/actions/download-artifact-4.3.0
* Merge pull request [#865](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/865) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.17
* Merge pull request [#867](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/867) from aws-powertools/dependabot/github_actions/zgosalvez/github-actions-ensure-sha-pinned-actions-3.0.24


<a name="1.50"></a>
## [1.50] - 2025-04-24
## Features

* enhance AppSyncEventsResolver with async handlers. Add documentation. Fix nullabel fields
* add JsonPropertyName attributes to AppSync event models for improved serialization
* update AppSyncEventsResolver to use async handlers and improve error handling
* refactor AppSync event handling to use nullable properties and rename event classes
* refactor AppSync event handling with nullable properties and improved class structure. Unauthorized exception and null subscribe responses
* enhance AppSyncEventsResolver with improved event handling and registration methods

## Maintenance

* **deps:** bump actions/setup-node from 4.3.0 to 4.4.0
* **deps:** bump aws-cdk-lib from 2.186.0 to 2.189.0
* **deps:** bump zgosalvez/github-actions-ensure-sha-pinned-actions
* **deps:** bump actions/setup-python from 5.4.0 to 5.5.0
* **deps:** bump github/codeql-action from 3.28.10 to 3.28.15
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump aws-cdk-lib from 2.180.0 to 2.186.0
* **deps:** bump actions/download-artifact from 4.1.9 to 4.2.1
* **deps:** bump actions/upload-artifact from 4.6.1 to 4.6.2

## Pull Requests

* Merge pull request [#858](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/858) from aws-powertools/feature/appsync-events
* Merge pull request [#848](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/848) from aws-powertools/dependabot/npm_and_yarn/aws-cdk-lib-2.189.0
* Merge pull request [#851](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/851) from aws-powertools/dependabot/github_actions/actions/setup-node-4.4.0
* Merge pull request [#849](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/849) from aws-powertools/update-changelog-14379009990
* Merge pull request [#847](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/847) from aws-powertools/fix/changelog-write
* Merge pull request [#830](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/830) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-23b69789b1dd836c53ea25b32f62ef8e1a23366037acd07c90959a219fd1f285
* Merge pull request [#838](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/838) from aws-powertools/dependabot/github_actions/zgosalvez/github-actions-ensure-sha-pinned-actions-3.0.23
* Merge pull request [#837](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/837) from aws-powertools/dependabot/github_actions/actions/setup-python-5.5.0
* Merge pull request [#836](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/836) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.15
* Merge pull request [#829](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/829) from aws-powertools/dependabot/npm_and_yarn/aws-cdk-lib-2.186.0
* Merge pull request [#825](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/825) from aws-powertools/dependabot/github_actions/actions/download-artifact-4.2.1
* Merge pull request [#823](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/823) from aws-powertools/dependabot/github_actions/actions/upload-artifact-4.6.2


<a name="1.40"></a>
## [1.40] - 2025-04-08
## Bug Fixes

* **build:** update ProjectReference condition to always include AWS.Lambda.Powertools.Common project
* **tests:** update AWS_EXECUTION_ENV version in assertions to 1.0.0

## Code Refactoring

* enhance log buffer management to discard oversized entries and improve entry tracking
* update logger factory and builder to support log output configuration
* update parameter names and improve documentation in logging configuration classes
* improve logging buffer management and configuration handling
* replace SystemWrapper with ConsoleWrapper in tests and update logging methods. revert systemwrapper, revert lambda.core to 2.5.0
* replace SystemWrapper with ConsoleWrapper in tests and update logging methods. revert systemwrapper, revert lambda.core to 2.5.0
* enhance logger configuration and output handling. Fix tests
* update log buffering options and improve serializer handling
* clean up whitespace and improve logger configuration handling
* change Logger class to static and enhance logging capabilities
* **logging:** enhance IsEnabled method for improved log level handling

## Features

* **console:** enhance ConsoleWrapper for test mode and output management
* **lifecycle:** add LambdaLifecycleTracker to manage cold start state and initialization type
* **logger:** enhance random number generation and improve regex match timeout
* **logging:** introduce custom logger output and enhance configuration options
* **logging:** add GetLogOutput method and CompositeJsonTypeInfoResolver for enhanced logging capabilities
* **workflows:** update .NET version setup to support multiple versions and improve package handling
* **workflows:** add examples tests and publish packages workflow; remove redundant test step

## Maintenance

* update Microsoft.Extensions.DependencyInjection to version 8.0.1
* **deps:** bump actions/setup-node from 4.2.0 to 4.3.0
* **deps:** bump actions/setup-dotnet from 4.3.0 to 4.3.1
* **deps:** update AWS Lambda Powertools packages to latest versions

## Pull Requests

* Merge pull request [#844](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/844) from hjgraca/fix/revert-common-setup
* Merge pull request [#843](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/843) from hjgraca/fix/batch-example-nuget-update
* Merge pull request [#842](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/842) from hjgraca/fix/update-example-nuget
* Merge pull request [#841](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/841) from hjgraca/fix/execution-env-ignore-version
* Merge pull request [#840](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/840) from hjgraca/fix/execution-env-version
* Merge pull request [#832](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/832) from hjgraca/feature/logger-ilogger-instance
* Merge pull request [#821](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/821) from aws-powertools/dependabot/github_actions/actions/setup-node-4.3.0
* Merge pull request [#820](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/820) from aws-powertools/dependabot/github_actions/actions/setup-dotnet-4.3.1
* Merge pull request [#835](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/835) from hjgraca/fix/override-lambda-console
* Merge pull request [#834](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/834) from hjgraca/feature/coldstart-provisioned-concurrency
* Merge pull request [#814](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/814) from hjgraca/chore/update-examples-130
* Merge pull request [#813](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/813) from hjgraca/chore/update-examples-130


<a name="1.30"></a>
## [1.30] - 2025-03-07
## Bug Fixes

* **build:** simplify dependency installation step in CI configuration
* **build:** pass target framework properties during restore, build, and test steps
* **build:** update test commands and project configurations for .NET frameworks
* **build:** add SkipInvalidProjects property to build properties for .NET frameworks
* **build:** add /tl option to dotnet build command in build.yml
* **build:** update .NET setup step to use matrix variable for versioning
* **ci:** Permissions ([#782](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/782))
* **ci:** Permissions and depdendencies
* **ci:** add write for issues
* **ci:** Add permissions to read issues and pull requests
* **ci:** label PRs
* **ci:** Workflow permissions ([#774](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/774))
* **ci:** Indentation issue
* **metrics:** add null checks and unit tests for MetricsAspect and MetricsAttribute
* **metrics:** rename variable for default dimensions in cold start handling
* **metrics:** ensure thread safety by locking metrics during cold start flag reset
* **tests:** correct command in e2e-tests.yml and remove unnecessary assertions in FunctionTests.cs
* **tests:** conditionally include project reference for net8.0 framework

## Code Refactoring

* **metrics:** simplify MetricsTests by removing unused variables and improving syntax
* **metrics:** standardize parameter names for clarity in metric methods
* **metrics:** standardize parameter names for metric methods to improve clarity

## Documentation

* **metrics:** document breaking changes in metrics output format and default dimensions

## Features

* **build:** increase verbosity for test and example runs in CI pipeline
* **build:** enhance CI configuration with multi-framework support for .NET 6.0 and 8.0
* **ci:** Permissions updates
* **metrics:** enhance cold start handling with default dimensions and add corresponding tests
* **metrics:** enhance WithFunctionName method to handle null or empty values and add corresponding unit tests
* **metrics:** update metrics to version 2.0.0, enhance cold start tracking, and improve documentation
* **metrics:** update default dimensions handling and increase maximum dimensions limit
* **metrics:** add Metrics.AspNetCore version to version.json
* **metrics:** add ColdStartTracker for tracking cold starts in ASP.NET Core applications
* **metrics:** enhance default dimensions handling and refactor metrics initialization. Adding default dimensions to cold start metrics
* **metrics:** implement IConsoleWrapper for abstracting console operations and enhance cold start metric capturing
* **metrics:** add unit tests for Metrics constructor and validation methods
* **metrics:** always set namespace and service, update tests for service handling
* **metrics:** add HandlerEmpty method and test for empty metrics exception handling
* **metrics:** add HandlerRaiseOnEmptyMetrics method and corresponding test for empty metrics exception
* **metrics:** enhance documentation for Cold Start Function Name dimension and update test classes
* **metrics:** add support for disabling metrics via environment variable
* **metrics:** add function name support for metrics dimensions
* **metrics:** add support for default dimensions in metrics handling
* **metrics:** introduce MetricsOptions for configurable metrics setup and refactor initialization logic
* **metrics:** add ASP.NET Core metrics package with cold start tracking and middleware support for aspnetcore. Docs
* **metrics:** enhance MetricsBuilder with detailed configuration options and improve documentation
* **metrics:** add MetricsBuilder for fluent configuration of metrics options and enhance default dimensions handling
* **metrics:** update TargetFramework to net8.0 and adjust MaxDimensions limit
* **tests:** add unit tests for ConsoleWrapper and Metrics middleware extensions
* **version:** update Metrics version to 2.0.0 in version.json

## Maintenance

* Add openssf scorecard badge to readme ([#790](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/790))
* **deps:** bump jinja2 from 3.1.5 to 3.1.6
* **deps:** bump jinja2 from 3.1.5 to 3.1.6 in /docs
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump codecov/codecov-action from 5.3.1 to 5.4.0
* **deps:** bump github/codeql-action from 3.28.9 to 3.28.10
* **deps:** bump ossf/scorecard-action from 2.4.0 to 2.4.1
* **deps:** bump actions/upload-artifact from 4.6.0 to 4.6.1
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump zgosalvez/github-actions-ensure-sha-pinned-actions
* **deps:** bump squidfunk/mkdocs-material in /docs

## Pull Requests

* Merge pull request [#811](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/811) from aws-powertools/chore/update-version
* Merge pull request [#810](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/810) from aws-powertools/fix-release-drafter
* Merge pull request [#807](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/807) from hjgraca/fix/metrics-namespace-service-not-present
* Merge pull request [#805](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/805) from aws-powertools/dependabot/pip/jinja2-3.1.6
* Merge pull request [#804](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/804) from aws-powertools/dependabot/pip/docs/jinja2-3.1.6
* Merge pull request [#802](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/802) from hjgraca/fix/metrics-e2e-tests
* Merge pull request [#801](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/801) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-047452c6641137c9caa3647d050ddb7fa67b59ed48cc67ec3a4995f3d360ab32
* Merge pull request [#800](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/800) from hjgraca/fix/low-hanging-fruit-metrics-v2
* Merge pull request [#799](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/799) from aws-powertools/maintenance/workflow-branch-develop
* Merge pull request [#797](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/797) from aws-powertools/fix-version-comma
* Merge pull request [#793](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/793) from aws-powertools/dependabot/github_actions/codecov/codecov-action-5.4.0
* Merge pull request [#791](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/791) from gregsinclair42/CheckForValidLambdaContext
* Merge pull request [#786](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/786) from hjgraca/feature/metrics-disabled
* Merge pull request [#785](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/785) from hjgraca/feature/metrics-function-name
* Merge pull request [#780](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/780) from hjgraca/feature/metrics-single-default-dimensions
* Merge pull request [#775](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/775) from hjgraca/feature/metrics-aspnetcore
* Merge pull request [#771](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/771) from hjgraca/feature/metrics-default-dimensions-coldstart
* Merge pull request [#789](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/789) from aws-powertools/permissions
* Merge pull request [#788](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/788) from aws-powertools/pr_merge
* Merge pull request [#787](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/787) from aws-powertools/indentation
* Merge pull request [#767](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/767) from aws-powertools/maintenance/sitemap
* Merge pull request [#778](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/778) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.10
* Merge pull request [#777](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/777) from aws-powertools/dependabot/github_actions/ossf/scorecard-action-2.4.1
* Merge pull request [#776](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/776) from aws-powertools/dependabot/github_actions/actions/upload-artifact-4.6.1
* Merge pull request [#770](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/770) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-26153027ff0b192d3dbea828f2fe2dd1bf6ff753c58dd542b3ddfe866b08bf60
* Merge pull request [#666](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/666) from hjgraca/fix(metrics)-dimessions-with-missing-array
* Merge pull request [#768](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/768) from aws-powertools/dependabot/github_actions/zgosalvez/github-actions-ensure-sha-pinned-actions-3.0.22
* Merge pull request [#764](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/764) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-f5bcec4e71c138bcb89c0dccb633c830f54a0218e1aefedaade952b61b908d00


<a name="1.20"></a>
## [1.20] - 2025-02-11
## Features

* **idempotency:** add support for custom key prefixes in IdempotencyHandler and related tests
* **tests:** add unit tests for IdempotencySerializer and update JSON options handling

## Maintenance

* add openssf scorecard workflow
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump actions/upload-artifact from 4.5.0 to 4.6.0
* **deps:** bump github/codeql-action from 3.28.8 to 3.28.9
* **deps:** bump zgosalvez/github-actions-ensure-sha-pinned-actions
* **deps:** bump aws-actions/configure-aws-credentials
* **deps:** bump squidfunk/mkdocs-material in /docs
* **deps:** bump github/codeql-action from 3.27.9 to 3.28.9
* **deps:** bump github/codeql-action from 3.28.6 to 3.28.8
* **deps:** bump actions/setup-dotnet from 4.2.0 to 4.3.0
* **deps:** bump github/codeql-action from 3.28.5 to 3.28.6
* **deps:** bump actions/setup-python from 5.3.0 to 5.4.0
* **deps:** bump aws-actions/configure-aws-credentials
* **deps:** bump pygments from 2.13.0 to 2.15.0

## Pull Requests

* Merge pull request [#755](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/755) from aws-powertools/dependabot/github_actions/aws-actions/configure-aws-credentials-4.1.0
* Merge pull request [#754](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/754) from aws-powertools/dependabot/github_actions/actions/upload-artifact-4.6.0
* Merge pull request [#753](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/753) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.9
* Merge pull request [#757](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/757) from hjgraca/docs/roadmap-2025-update
* Merge pull request [#758](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/758) from aws-powertools/docs/idempotency-prefix
* Merge pull request [#743](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/743) from aws-powertools/release(1.20)-update-versions
* Merge pull request [#355](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/355) from aws-powertools/dependabot/pip/pygments-2.15.0
* Merge pull request [#751](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/751) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.9
* Merge pull request [#750](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/750) from aws-powertools/dependabot/github_actions/zgosalvez/github-actions-ensure-sha-pinned-actions-3.0.21
* Merge pull request [#748](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/748) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-c62453b1ba229982c6325a71165c1a3007c11bd3dd470e7a1446c5783bd145b4
* Merge pull request [#745](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/745) from hjgraca/feature/idempotency-key-prefix
* Merge pull request [#747](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/747) from aws-powertools/mkdocs/privacy-plugin
* Merge pull request [#653](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/653) from hjgraca/aot(idempotency|jmespath)-aot-support
* Merge pull request [#744](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/744) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-7e841df1cfb6c8c4ff0968f2cfe55127fb1a2f5614e1c9bc23cbc11fe4c96644
* Merge pull request [#738](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/738) from hjgraca/feat(e2e)-idempotency-e2e-tests
* Merge pull request [#741](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/741) from hjgraca/fix(tracing)-invalid-sement-name
* Merge pull request [#739](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/739) from aws-powertools/dependabot/docker/docs/squidfunk/mkdocs-material-471695f3e611d9858788ac04e4daa9af961ccab73f1c0f545e90f8cc5d4268b8
* Merge pull request [#736](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/736) from aws-powertools/dependabot/github_actions/actions/setup-dotnet-4.3.0
* Merge pull request [#737](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/737) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.8
* Merge pull request [#734](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/734) from aws-powertools/fix-apidocs-build
* Merge pull request [#727](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/727) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.6
* Merge pull request [#725](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/725) from aws-powertools/dependabot/github_actions/aws-actions/configure-aws-credentials-4.0.3
* Merge pull request [#726](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/726) from aws-powertools/dependabot/github_actions/actions/setup-python-5.4.0
* Merge pull request [#731](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/731) from aws-powertools/patch-do-not-pack-tests


<a name="1.19"></a>
## [1.19] - 2025-01-28
## Maintenance

* **deps:** bump codecov/codecov-action from 5.3.0 to 5.3.1
* **deps:** bump github/codeql-action from 3.28.4 to 3.28.5
* **deps:** bump actions/upload-artifact from 4.5.0 to 4.6.0
* **deps:** bump actions/checkout from 4.1.7 to 4.2.2
* **deps:** bump zgosalvez/github-actions-ensure-sha-pinned-actions
* **deps:** bump release-drafter/release-drafter from 5.21.1 to 6.1.0
* **deps:** bump codecov/codecov-action from 4.5.0 to 5.3.0
* **deps:** bump actions/github-script from 6 to 7
* **deps:** bump github/codeql-action from 2.1.18 to 3.28.4
* **deps:** bump actions/upload-artifact from 3 to 4
* **deps:** bump aws-actions/configure-aws-credentials
* **deps:** bump actions/setup-dotnet from 3.0.3 to 4.2.0

## Pull Requests

* Merge pull request [#728](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/728) from aws-powertools/hjgraca-docs-service
* Merge pull request [#724](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/724) from aws-powertools/release(1.19)-update-versions
* Merge pull request [#704](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/704) from hjgraca/fix(logging)-service-name-override
* Merge pull request [#722](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/722) from aws-powertools/dependabot/github_actions/codecov/codecov-action-5.3.1
* Merge pull request [#721](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/721) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.5
* Merge pull request [#714](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/714) from aws-powertools/dependabot/github_actions/codecov/codecov-action-5.3.0
* Merge pull request [#715](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/715) from aws-powertools/dependabot/github_actions/release-drafter/release-drafter-6.1.0
* Merge pull request [#716](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/716) from aws-powertools/dependabot/github_actions/zgosalvez/github-actions-ensure-sha-pinned-actions-3.0.20
* Merge pull request [#717](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/717) from aws-powertools/dependabot/github_actions/actions/checkout-4.2.2
* Merge pull request [#720](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/720) from aws-powertools/chore/e2e-libraries-path
* Merge pull request [#718](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/718) from aws-powertools/dependabot/github_actions/actions/upload-artifact-4.6.0
* Merge pull request [#713](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/713) from aws-powertools/chore(e2e)-concurrency
* Merge pull request [#707](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/707) from aws-powertools/dependabot/github_actions/actions/setup-dotnet-4.2.0
* Merge pull request [#708](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/708) from aws-powertools/dependabot/github_actions/aws-actions/configure-aws-credentials-4.0.2
* Merge pull request [#711](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/711) from aws-powertools/dependabot/github_actions/actions/github-script-7
* Merge pull request [#710](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/710) from aws-powertools/dependabot/github_actions/github/codeql-action-3.28.4
* Merge pull request [#709](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/709) from aws-powertools/dependabot/github_actions/actions/upload-artifact-4
* Merge pull request [#706](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/706) from aws-powertools/ci/dependabot
* Merge pull request [#700](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/700) from hjgraca/hjgraca-e2e-aot
* Merge pull request [#679](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/679) from hjgraca/dep(examples)-update-examples-dep
* Merge pull request [#682](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/682) from aws-powertools/dependabot/pip/jinja2-3.1.5
* Merge pull request [#699](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/699) from hjgraca/aot-e2e-tests
* Merge pull request [#698](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/698) from ankitdhaka07/issue-697


<a name="1.18"></a>
## [1.18] - 2025-01-14
## Pull Requests

* Merge pull request [#695](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/695) from aws-powertools/update-versio-release118
* Merge pull request [#692](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/692) from hjgraca/feature/e2etests
* Merge pull request [#691](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/691) from aws-powertools/hjgraca-patch-e2e-6
* Merge pull request [#690](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/690) from aws-powertools/hjgraca-patch-e2e-5
* Merge pull request [#689](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/689) from aws-powertools/hjgraca-patch-e2e-4
* Merge pull request [#688](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/688) from aws-powertools/hjgraca-patch-e2e-3
* Merge pull request [#687](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/687) from aws-powertools/hjgraca-patch-e2e-2
* Merge pull request [#686](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/686) from aws-powertools/hjgraca-patch-e2e
* Merge pull request [#685](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/685) from hjgraca/feat-e2e
* Merge pull request [#684](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/684) from hjgraca/feature/e2etests
* Merge pull request [#681](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/681) from hjgraca/feat(logging)-inner-exception


<a name="1.17"></a>
## [1.17] - 2024-11-12
## Pull Requests

* Merge pull request [#675](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/675) from hjgraca/fix(tracing)-aot-void-task-and-serialization


<a name="1.16"></a>
## [1.16] - 2024-10-22
## Pull Requests

* Merge pull request [#672](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/672) from aws-powertools/hjgraca-logging-release115
* Merge pull request [#670](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/670) from hjgraca/fix(logging)-enum-serialization
* Merge pull request [#664](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/664) from hjgraca/fix(metrics)-multiple-dimension-array


<a name="1.15"></a>
## [1.15] - 2024-10-05
## Pull Requests

* Merge pull request [#660](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/660) from hjgraca/fix(tracing)-revert-imethodaspecthander-removal
* Merge pull request [#657](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/657) from hjgraca/fix(logging)-typeinforesolver-non-aot
* Merge pull request [#646](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/646) from lachriz-aws/feature/throw-on-full-batch-failure-option
* Merge pull request [#652](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/652) from hjgraca/chore(dependencies)-update-logging-examples


<a name="1.14"></a>
## [1.14] - 2024-09-24
## Pull Requests

* Merge pull request [#649](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/649) from hjgraca/(docs)-update-logging-aot
* Merge pull request [#628](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/628) from hjgraca/aot(logging)-support-logging
* Merge pull request [#645](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/645) from aws-powertools/chore(examples)Update-examples-release-1.13
* Merge pull request [#643](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/643) from hjgraca/fix(dependencies)-Fix-Common-dependency
* Merge pull request [#641](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/641) from hjgraca/fix(references)-build-targets-common


<a name="1.13"></a>
## [1.13] - 2024-08-29
## Maintenance

* **docs:** load self hosted mermaid.js
* **docs:** load self hosted mermaid.js
* **docs:** Caylent customer reference

## Pull Requests

* Merge pull request [#639](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/639) from aws-powertools/fix(docs)-missing-closing-tag
* Merge pull request [#638](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/638) from aws-powertools/release(1.13)-update-versions
* Merge pull request [#622](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/622) from aws-powertools/fix-typo-tracing-docs
* Merge pull request [#632](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/632) from hjgraca/fix(tracing)-batch-handler-result-null-reference
* Merge pull request [#633](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/633) from hjgraca/publicref/pushpay
* Merge pull request [#627](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/627) from hjgraca/fix-idempotency-jmespath-dependency
* Merge pull request [#625](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/625) from hjgraca/docs(public_reference)-add-Caylent-as-a-public-reference
* Merge pull request [#623](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/623) from hjgraca/chore-update-tracing-examples-150


<a name="1.12"></a>
## [1.12] - 2024-07-24
## Maintenance

* **deps-dev:** bump zipp from 3.11.0 to 3.19.1

## Pull Requests

* Merge pull request [#607](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/607) from hjgraca/aot-tracing-support
* Merge pull request [#610](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/610) from aws-powertools/dependabot/pip/zipp-3.19.1
* Merge pull request [#617](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/617) from hjgraca/example-update-release-1.11.1


<a name="1.11.1"></a>
## [1.11.1] - 2024-07-12
## Pull Requests

* Merge pull request [#613](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/613) from hjgraca/fix-metrics-resolution-context


<a name="1.10.2"></a>
## [1.10.2] - 2024-07-09

<a name="1.11"></a>
## [1.11] - 2024-07-09
## Maintenance

* **deps:** bump jinja2 from 3.1.3 to 3.1.4

## Pull Requests

* Merge pull request [#579](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/579) from aws-powertools/dependabot/pip/jinja2-3.1.4
* Merge pull request [#602](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/602) from hjgraca/aot-metrics-support
* Merge pull request [#605](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/605) from aws-powertools/hjgraca-codecov
* Merge pull request [#600](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/600) from aws-powertools/hjgraca-examples-1.10.1


<a name="1.10.1"></a>
## [1.10.1] - 2024-05-22
## Pull Requests

* Merge pull request [#596](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/596) from aws-powertools/hjgraca-update-version-1.10.1
* Merge pull request [#594](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/594) from hjgraca/metrics-thread-safety-bug
* Merge pull request [#589](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/589) from aws-powertools/hjgraca-idempotency-examples
* Merge pull request [#590](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/590) from hjgraca/fix-jmespath-dep


<a name="1.10.0"></a>
## [1.10.0] - 2024-05-09

<a name="1.9.2"></a>
## [1.9.2] - 2024-05-09
## Documentation

* add link to Powertools for AWS Lambda workshop

## Pull Requests

* Merge pull request [#586](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/586) from aws-powertools/hjgraca-version-release-1-10
* Merge pull request [#578](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/578) from hjgraca/feature/jmespath-powertools
* Merge pull request [#584](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/584) from aws-powertools/hjgraca-build-pipeline
* Merge pull request [#581](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/581) from dreamorosi/docs/link_workshop


<a name="1.9.1"></a>
## [1.9.1] - 2024-03-21
## Pull Requests

* Merge pull request [#575](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/575) from aws-powertools/release-191
* Merge pull request [#572](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/572) from hjgraca/fix-tracing-duplicate-generic-method-decorator
* Merge pull request [#569](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/569) from aws-powertools/hjgraca-update-docs-dotnet8


<a name="1.9.0"></a>
## [1.9.0] - 2024-03-11
## Pull Requests

* Merge pull request [#565](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/565) from aws-powertools/update-nuget-examples
* Merge pull request [#564](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/564) from amirkaws/update-nuget-versions-for-examples
* Merge pull request [#563](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/563) from amirkaws/release-version-1.9.0
* Merge pull request [#561](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/561) from amirkaws/update-nuget-versions
* Merge pull request [#555](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/555) from aws-powertools/hjgraca-update-examples-185
* Merge pull request [#559](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/559) from amirkaws/add-configuration-parameter-provider


<a name="1.8.5"></a>
## [1.8.5] - 2024-02-16
## Documentation

* updated we made this section with video series from Rahul and workshops

## Maintenance

* **deps:** bump jinja2 from 3.1.2 to 3.1.3
* **deps:** bump gitpython from 3.1.37 to 3.1.41

## Pull Requests

* Merge pull request [#552](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/552) from aws-powertools/hjgraca-update-version-185
* Merge pull request [#538](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/538) from hjgraca/hendle-exception-logger
* Merge pull request [#547](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/547) from aws-powertools/hjgraca-batch-docs
* Merge pull request [#548](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/548) from H1Gdev/doc
* Merge pull request [#542](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/542) from hjgraca/dotnet8-support
* Merge pull request [#539](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/539) from aws-powertools/dependabot/pip/gitpython-3.1.41
* Merge pull request [#540](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/540) from aws-powertools/dependabot/pip/jinja2-3.1.3
* Merge pull request [#544](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/544) from aws-powertools/hjgraca-docs-auto-disable-tracing
* Merge pull request [#536](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/536) from sliedig/develop


<a name="1.8.4"></a>
## [1.8.4] - 2023-12-12
## Pull Requests

* Merge pull request [#532](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/532) from aws-powertools/hjgraca-update-batch-ga
* Merge pull request [#528](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/528) from aws-powertools/idempotency-183-examples


<a name="1.8.3"></a>
## [1.8.3] - 2023-11-21
## Pull Requests

* Merge pull request [#525](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/525) from aws-powertools/idempotency-ga
* Merge pull request [#523](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/523) from hjgraca/update-examples-182
* Merge pull request [#513](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/513) from hjgraca/idempotency-method-e2e-test
* Merge pull request [#521](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/521) from hjgraca/182-fix-examples-logging-batch


<a name="1.8.2"></a>
## [1.8.2] - 2023-11-16
## Pull Requests

* Merge pull request [#518](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/518) from aws-powertools/hjgraca-version-1.8.2
* Merge pull request [#516](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/516) from hjgraca/lambda-log-level
* Merge pull request [#510](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/510) from aws-powertools/hjgraca-examples-1.8.1


<a name="1.8.1"></a>
## [1.8.1] - 2023-10-30
## Maintenance

* **deps:** bump gitpython from 3.1.35 to 3.1.37

## Pull Requests

* Merge pull request [#507](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/507) from aws-powertools/hjgraca-release-1.8.1
* Merge pull request [#505](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/505) from hjgraca/fix-exception-addmetadata
* Merge pull request [#499](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/499) from hjgraca/metrics-decorator-exception
* Merge pull request [#503](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/503) from hjgraca/dateonly-converter
* Merge pull request [#502](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/502) from aws-powertools/dependabot/pip/gitpython-3.1.37
* Merge pull request [#495](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/495) from hjgraca/update-projects-readme
* Merge pull request [#493](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/493) from hjgraca/release1.8.0-example-updates
* Merge pull request [#492](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/492) from aws-powertools/update-changelog-6248167844


<a name="1.8.0"></a>
## [1.8.0] - 2023-09-20
## Documentation

* add kinesis and dynamodb

## Pull Requests

* Merge pull request [#489](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/489) from amirkaws/release-version-1.8.0
* Merge pull request [#337](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/337) from lachriz-aws/feature/batch-processing


<a name="1.7.1"></a>
## [1.7.1] - 2023-09-19
## Pull Requests

* Merge pull request [#486](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/486) from amirkaws/update-examples-nuget-versions
* Merge pull request [#484](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/484) from aws-powertools/hjgraca-release-1.7.1
* Merge pull request [#482](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/482) from aws-powertools/hjgraca-release-1.7.1
* Merge pull request [#480](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/480) from hjgraca/bug-revert-aspectinjector
* Merge pull request [#479](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/479) from aws-powertools/hjgraca-update-examples-1.7.0
* Merge pull request [#477](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/477) from aws-powertools/hjgraca-delete-dependabot


<a name="1.7.0"></a>
## [1.7.0] - 2023-09-14
## Maintenance

* **deps:** bump gitpython from 3.1.30 to 3.1.35

## Pull Requests

* Merge pull request [#475](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/475) from aws-powertools/hjgraca-disable-dependabot
* Merge pull request [#464](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/464) from aws-powertools/hjgraca-release-1.7.0
* Merge pull request [#451](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/451) from aws-powertools/dependabot/pip/gitpython-3.1.35
* Merge pull request [#340](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/340) from hjgraca/fix-common-dependency
* Merge pull request [#437](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/437) from aws-powertools/update-changelog-6143683791
* Merge pull request [#435](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/435) from amirkaws/release-version-1.6.0-update-examples


<a name="1.6.0"></a>
## [1.6.0] - 2023-09-07
## Pull Requests

* Merge pull request [#433](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/433) from amirkaws/release-version-1.6.0-in-preview-fix
* Merge pull request [#432](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/432) from amirkaws/release-version-1.6.0-document-fix
* Merge pull request [#430](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/430) from amirkaws/release-version-1.6.0
* Merge pull request [#428](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/428) from amirkaws/automatic-xray-register
* Merge pull request [#400](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/400) from aws-powertools/hjgraca-fix-idempotency-docs
* Merge pull request [#391](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/391) from aws-powertools/hjgraca-patch-dependabot


<a name="1.5.0"></a>
## [1.5.0] - 2023-08-29
## Pull Requests

* Merge pull request [#397](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/397) from aws-powertools/hjgraca-version-1.5.0
* Merge pull request [#363](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/363) from hjgraca/idempotency-inprogressexpiration


<a name="1.4.2"></a>
## [1.4.2] - 2023-08-22
## Maintenance

* **deps:** bump AWSSDK.SecretsManager in /libraries
* **deps:** bump xunit from 2.4.1 to 2.4.2 in /libraries
* **deps:** bump Moq from 4.18.1 to 4.18.4 in /libraries
* **deps:** bump AWSSDK.DynamoDBv2 in /libraries
* **deps:** bump Testcontainers from 3.2.0 to 3.3.0 in /libraries
* **deps:** bump AWSXRayRecorder.Core in /libraries

## Pull Requests

* Merge pull request [#388](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/388) from amirkaws/update-samples-nuget-versions
* Merge pull request [#383](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/383) from aws-powertools/hjgraca-patch-version-1.4.2
* Merge pull request [#381](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/381) from aws-powertools/hjgraca-patch-dependabot
* Merge pull request [#375](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/375) from amirkaws/custom-log-formatter
* Merge pull request [#357](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/357) from hjgraca/fix-capture-stacktrace
* Merge pull request [#343](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/343) from aws-powertools/update-changelog-5462167625
* Merge pull request [#370](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/370) from amirkaws/replace-moq-with-nsubstitute
* Merge pull request [#359](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/359) from aws-powertools/dependabot/nuget/libraries/develop/AWSSDK.SecretsManager-3.7.200.3
* Merge pull request [#342](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/342) from hjgraca/idempotency-simpler-example
* Merge pull request [#347](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/347) from hjgraca/docs-clarify-xray-over-adot
* Merge pull request [#338](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/338) from aws-powertools/hjgraca-patch-boring-cyborg
* Merge pull request [#349](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/349) from hossambarakat/feature/idempotent-function
* Merge pull request [#328](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/328) from aws-powertools/dependabot/nuget/libraries/develop/xunit-2.4.2
* Merge pull request [#327](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/327) from aws-powertools/dependabot/nuget/libraries/develop/AWSSDK.DynamoDBv2-3.7.104.1
* Merge pull request [#325](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/325) from aws-powertools/dependabot/nuget/libraries/develop/AWSXRayRecorder.Core-2.14.0
* Merge pull request [#326](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/326) from aws-powertools/dependabot/nuget/libraries/develop/Testcontainers-3.3.0
* Merge pull request [#329](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/329) from aws-powertools/dependabot/nuget/libraries/develop/Moq-4.18.4


<a name="1.4.1"></a>
## [1.4.1] - 2023-06-29
## Maintenance

* remove GH pages

## Pull Requests

* Merge pull request [#318](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/318) from aws-powertools/hjgraca-dependabot-location
* Merge pull request [#324](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/324) from amirkaws/release-version-1.4.1
* Merge pull request [#320](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/320) from aws-powertools/hjgraca-idempotency-example-fix
* Merge pull request [#319](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/319) from aws-powertools/hjgraca-idempotency-example-DynamoDBv2-version
* Merge pull request [#312](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/312) from hjgraca/metrics-prevent-exceed-100-datapoint
* Merge pull request [#317](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/317) from aws-powertools/hjgraca-add-dependabot
* Merge pull request [#300](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/300) from hossambarakat/feature/idempotency-example
* Merge pull request [#298](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/298) from amirkaws/parameters-example
* Merge pull request [#315](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/315) from aws-powertools/url-updates
* Merge pull request [#314](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/314) from aws-powertools/readme-updates
* Merge pull request [#313](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/313) from hjgraca/metrics-addmetric-raceconditiom
* Merge pull request [#287](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/287) from swimming-potato/develop
* Merge pull request [#303](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/303) from aws-powertools/remove-gh-pages
* Merge pull request [#308](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/308) from aws-powertools/update-changelog-5332675096


<a name="1.4.0"></a>
## [1.4.0] - 2023-06-21
## Bug Fixes

* update team name
* update references
* updated codeowners
* reapplying some lins that got screwed up in the merge

## Documentation

* adding permission

## Features

* **docs:** Start S3 Docs

## Maintenance

* updated code owners
* Change repo URL to the new location
* rename project to Powertools for AWS Lambda (.NET)
* **ci:** updated links to new repo
* **ci:** removed unnecessary areas
* **docs:** fix we made this link
* **docs:** update docs homepage with additional features, fixed dot cli commands, new SAM cli instructions
* **docs:** updated readme with idempotency package and examples for parameters and idempotency
* **docs:** move idempotency

## Pull Requests

* Merge pull request [#305](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/305) from aws-powertools/version-bump-1.4
* Merge pull request [#301](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/301) from sliedig/sliedig-docs
* Merge pull request [#302](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/302) from aws-powertools/rename-part2
* Merge pull request [#291](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/291) from aws-powertools/doc-updates-roadmap
* Merge pull request [#285](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/285) from aws-powertools/url-rename
* Merge pull request [#293](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/293) from glynn1211/develop
* Merge pull request [#163](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/163) from hossambarakat/feature/idempotency
* Merge pull request [#282](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/282) from awslabs/rename
* Merge pull request [#277](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/277) from awslabs/update-changelog-4981653012
* Merge pull request [#274](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/274) from awslabs/dependabot/pip/pymdown-extensions-10.0
* Merge pull request [#276](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/276) from awslabs/pymdown-extension-fix
* Merge pull request [#278](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/278) from awslabs/s3-docs
* Merge pull request [#273](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/273) from leandrodamascena/parameters/docs
* Merge pull request [#271](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/271) from amirkaws/amirkaws-fix-parameters-nuget-icon


<a name="1.3.0"></a>
## [1.3.0] - 2023-05-12
## Documentation

* fixed formatting and updated content

## Features

* add package readme

## Maintenance

* **ci:** skip analytics on forks

## Pull Requests

* Merge pull request [#268](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/268) from amirkaws/amirkaws-release-version-1.3.0
* Merge pull request [#167](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/167) from amirkaws/amirkaws-feature-parameters
* Merge pull request [#1](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/1) from sliedig/amirkaws-feature-parameters
* Merge pull request [#264](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/264) from awslabs/chore(ci)-skip-analytics-on-forks
* Merge pull request [#262](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/262) from awslabs/chorebump-version-1.2.0-release


<a name="1.2.0"></a>
## [1.2.0] - 2023-05-05
## Pull Requests

* Merge pull request [#258](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/258) from amirkaws/amirkaws-release-version-1.2.0
* Merge pull request [#226](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/226) from hjgraca/feat_support_high_resolution_metrics


<a name="1.1.0"></a>
## [1.1.0] - 2023-05-05
## Maintenance

* add Lambda Powertools for Python in issue templates
* add workflow to dispatch analytics fetching
* **ci:** add workflow to dispatch analytics fetching

## Pull Requests

* Merge pull request [#255](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/255) from awslabs/fix-remove-real-env-tests
* Merge pull request [#253](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/253) from amirkaws/amirkaws-release-v1.1.0
* Merge pull request [#246](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/246) from hjgraca/feat_set-execution-context
* Merge pull request [#251](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/251) from leandrodamascena/issues-templates/python
* Merge pull request [#241](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/241) from awslabs/update-changelog-4691350388
* Merge pull request [#237](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/237) from hjgraca/changelog-update-pipeline
* Merge pull request [#235](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/235) from amirkaws/amirkaws-update-examples-nuget-references-release-v1.0.1


<a name="v1.0.1"></a>
## [v1.0.1] - 2023-04-06
## Pull Requests

* Merge pull request [#232](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/232) from amirkaws/amirkaws-release-v1.0.1
* Merge pull request [#227](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/227) from hjgraca/chore_fix_changelog_build
* Merge pull request [#225](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/225) from srcsakthivel/develop
* Merge pull request [#223](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/223) from hjgraca/fix_tracing_on_exception_thrown
* Merge pull request [#218](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/218) from amirkaws/amirkaws-update-examples-nuget-references-release


<a name="v1.0.0"></a>
## [v1.0.0] - 2023-02-24
## Bug Fixes

* removing manual trigger on docs wf

## Documentation

* **home:** update powertools definition

## Maintenance

* **ci:** api docs build update ([#188](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/188))
* **ci:** changing trigger to run manually
* **ci:** updated api docs implementation
* **ci:** updated bug report template
* **deps:** updates sample deps
* **docs:** incorrect crefs

## Pull Requests

* Merge pull request [#215](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/215) from amirkaws/amirkaws-release-v1.0.0
* Merge pull request [#208](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/208) from amirkaws/amirkaws-cold-start-capture-warning-bug-fix
* Merge pull request [#210](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/210) from awslabs/powertools-definition-update
* Merge pull request [#209](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/209) from amirkaws/amirkaws-metrics-timestamp-fix
* Merge pull request [#199](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/199) from awslabs/sliedig-ci-reviewers
* Merge pull request [#193](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/193) from hjgraca/maintenance-new-issue-template
* Merge pull request [#202](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/202) from hjgraca/fix-test-json-escaping
* Merge pull request [#195](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/195) from hjgraca/update-dotnet-sdk-6.0.405
* Merge pull request [#194](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/194) from hjgraca/patch-1
* Merge pull request [#192](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/192) from hjgraca/fix-incorrect-crefs
* Merge pull request [#189](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/189) from sliedig/develop
* Merge pull request [#185](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/185) from amirkaws/amirkaws-update-examples-nuget-references
* Merge pull request [#183](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/183) from awslabs/develop
* Merge pull request [#150](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/150) from awslabs/develop
* Merge pull request [#140](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/140) from awslabs/develop


<a name="v0.0.2-preview"></a>
## [v0.0.2-preview] - 2023-01-18
## Bug Fixes

* removed duplicate template issue
* updated logger casing env vars in samples

## Documentation

* typo in metrics README

## Features

* **ci:** codeql static code analysis ([#148](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/148))

## Maintenance

* updated setup-dotnet[@v1](https://github.com/v1) to [@v3](https://github.com/v3)
* updated packages ([#172](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/172))
* **ci:** bumped version
* **ci:** minor updates, licensing
* **deps:** bump gitpython from 3.1.29 to 3.1.30
* **docs:** updated documentation ([#175](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/175))
* **docs:** add discord invitation link

## Pull Requests

* Merge pull request [#182](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/182) from sliedig/develop
* Merge pull request [#181](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/181) from awslabs/dependabot/pip/gitpython-3.1.30
* Merge pull request [#179](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/179) from sliedig/sliedig-ci
* Merge pull request [#174](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/174) from sliedig/sliedig-ci
* Merge pull request [#173](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/173) from sliedig/sliedig-samples
* Merge pull request [#157](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/157) from kenfdev/fix-readme-title-typo
* Merge pull request [#170](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/170) from nCubed/develop
* Merge pull request [#152](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/152) from amirkaws/amirkaws-custom-exception-json-converter
* Merge pull request [#155](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/155) from sthuber90/add-discord-link-154
* Merge pull request [#147](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/147) from amirkaws/amirkaws-fix-doc-links


<a name="v0.0.1-preview.1"></a>
## v0.0.1-preview.1 - 2022-08-01
## Bug Fixes

* force directy rename
* making test function compile.
* updating issue templates with correct extention
* updated auto assign
* skip duplicate nuget packages publish
* added missing runtimes
* updated Logging template description
* updated documentation and doc generation ([#96](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/96))
* removed optional doc file paths
* explicitly adding doc files for build configurations
* resolving dependecy alert CVE-2020-8116
* added missing codecov packages for test projects
* fixed build
* forcing rename
* fixed powertolls spelling in docs
* replaced PackageIconUrl which is being depreciated with PackageIcon
* added missing include to pack README files
* fixing node vulnerabilites for docs
* resolving merge conflict
* update packages to resolve vulnerabilities. Switched to yarn package manager
* intermin fix to resolve vulnerability issues.
* proj references
* fixed spelling in libraries folder name
* **ci:** lockdown gh-pages workflow to sha

## Documentation

* fixed nav for roadmap
* updated link to feature request; added roadmap
* library readme updates and minor updates  ([#117](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/117))
* spell check with US-English dictionary ([#115](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/115))
* docs review ([#112](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/112))
* Reviewing documentation ([#68](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/68))
* adding auto-generated API Reference to docs ([#87](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/87))
* alternative brew installation ([#86](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/86))
* homebrew installation ([#85](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/85))
* merging api generation tasks ([#84](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/84))
* fixing docfx path ([#83](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/83))
* fix docfx path ([#82](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/82))
* fix api docs generator installation ([#81](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/81))
* update github actions to publish api docs ([#80](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/80))
* API docs generation ([#79](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/79))

## Features

* added security.md
* updated record_pr action
* PR Labeler GitHub actions ([#119](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/119))
* Logger output case attributes docs and unit testing ([#100](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/100))
* add extra fields to the logger methods ([#98](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/98))
* updated examples to include managed runtime configuration as well as docker. Made updates to Tracing implementation
* added Tracing example
* added init Metrics sample
* added Logging example
* added serialisation options to force dictionary keys to camel case
* added build tools to generate nuget packages
* updated project packaging properties
* added package README files for core utilities
* update make and doc dep to build docs
* added docs template

## Maintenance

* set global .net version
* temporarily removing docs while content is being developed
* added example project
* refctored PowerTools to Powertools
* added github files and templates
* initial folder structure
* interim resolution of docs package vulnerabilities
* migrated AWS.Lambda.PowerTools to AWS.Lambda.PowerTools.Common namespace. fix: resolved incorrect namespace for Tracing fix: resolved dependencies in example project
* refactored to new namespace
* updated readme
* updated github templates
* added customer 404 page
* removing unecessary buildtools
* update pr template
* updating type documentation and file headers ([#56](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/56))
* moved solution file into libraries. Need a separate solution for examples
* added docs
* added gitignore and updated licence
* updating doc build workflow
* updated issue templates
* updated Label PR based on title action
* deleted unnecessay publishing action
* updated stale action
* removed unnecessary pr labeling
* updated PR labeling path
* updated list of assignees
* updated docker builds to use Amazon.Lambda.Tools ([#118](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/118))
* bumped .net version in global to 6.0.301 ([#120](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/120))
* bumped .net version in global to 6.0.301
* adding missed copyright info
* added copyright to examples
* removed SimpleLambda from examples
* cleaned up logging and metrics functions
* **ci:** add on_merge_pr workflow to notify releases
* **ci:** lockdown untrusted workflows to sha
* **ci:** add missing scripts
* **ci:** updated bug report template ([#144](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/144))
* **ci:** add workflow to detect missing related issue
* **ci:** enable concurrency group for docs workflow
* **ci:** upudated wording in PR template checklist
* **ci:** added codeowners
* **ci:** added Maintainers doc
* **ci:** updated PR workflows and scripts
* **ci:** upgrade setup-python to v4
* **ci:** upgrade checkout action to v3
* **ci:** use untrusted workflows with sha
* **ci:** add reusable export pr workflow dependency
* **deps:** bump hosted-git-info from 2.8.8 to 2.8.9 in /docs
* **deps:** bump object-path from 0.11.4 to 0.11.5 in /docs
* **deps:** bump ssri from 6.0.1 to 6.0.2 in /docs
* **deps:** bump elliptic from 6.5.3 to 6.5.4 in /docs
* **deps:** bump socket.io from 2.3.0 to 2.4.1 in /docs
* **deps:** bump ini from 1.3.5 to 1.3.8 in /docs
* **deps:** bump ua-parser-js from 0.7.23 to 0.7.28 in /docs
* **deps:** bump underscore from 1.12.0 to 1.13.1 in /docs
* **deps:** bump url-parse from 1.4.7 to 1.5.1 in /docs
* **deps:** bump prismjs from 1.20.0 to 1.21.0 in /docs
* **deps:** updates sample deps ([#142](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/142))
* **deps:** bump mkdocs from 1.2.2 to 1.2.3 ([#29](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/29))
* **governance:** render debug logs with csharp syntax
* **governance:** typo in pending release label name

## Pull Requests

* Merge pull request [#139](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/139) from awslabs/amirkaws-resolve-conflicts-2
* Merge pull request [#135](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/135) from awslabs/amirkaws-update-versions
* Merge pull request [#134](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/134) from heitorlessa/chore/lockdown-gh-pages-workflow
* Merge pull request [#132](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/132) from heitorlessa/chore/github-concurrency-docs
* Merge pull request [#130](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/130) from heitorlessa/chore/enforce-github-actions-sha
* Merge pull request [#128](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/128) from sliedig/sliedig-ci
* Merge pull request [#127](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/127) from sliedig/sliedig-ci
* Merge pull request [#126](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/126) from sliedig/develop
* Merge pull request [#123](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/123) from sliedig/develop
* Merge pull request [#1](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/1) from sliedig/sliedig/develop
* Merge pull request [#121](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/121) from awslabs/amirkaws-update-versions
* Merge pull request [#116](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/116) from awslabs/amirkaws/add-di-support-for-logging
* Merge pull request [#113](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/113) from awslabs/amirkaws/update-doc-1
* Merge pull request [#111](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/111) from awslabs/amirkaws/add-env-vars-docs
* Merge pull request [#102](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/102) from sliedig/sliedig/examples
* Merge pull request [#103](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/103) from awslabs/amirkaws/fix-example-issues
* Merge pull request [#97](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/97) from sliedig/sliedig/examples
* Merge pull request [#95](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/95) from awslabs/pr/91
* Merge pull request [#90](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/90) from sliedig/develop
* Merge pull request [#89](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/89) from awslabs/api-docs-template
* Merge pull request [#74](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/74) from awslabs/amirkaws/disable-tracing-outside-lambda-env
* Merge pull request [#66](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/66) from sliedig/develop
* Merge pull request [#59](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/59) from sliedig/develop
* Merge pull request [#58](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/58) from sliedig/sliedig/nuget
* Merge pull request [#32](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/32) from awslabs/amirkaws/metrics-1
* Merge pull request [#31](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/31) from t1agob/develop
* Merge pull request [#2](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/2) from awslabs/develop
* Merge pull request [#25](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/25) from t1agob/develop
* Merge pull request [#1](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/1) from t1agob/sourcegenerators
* Merge pull request [#24](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/24) from sliedig/develop
* Merge pull request [#23](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/23) from sliedig/develop
* Merge pull request [#22](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/22) from sliedig/develop
* Merge pull request [#21](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/21) from t1agob/develop
* Merge pull request [#19](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/19) from awslabs/dependabot/npm_and_yarn/docs/url-parse-1.5.1
* Merge pull request [#18](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/18) from awslabs/dependabot/npm_and_yarn/docs/hosted-git-info-2.8.9
* Merge pull request [#17](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/17) from awslabs/dependabot/npm_and_yarn/docs/ua-parser-js-0.7.28
* Merge pull request [#16](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/16) from awslabs/dependabot/npm_and_yarn/docs/underscore-1.13.1
* Merge pull request [#15](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/15) from awslabs/dependabot/npm_and_yarn/docs/ssri-6.0.2
* Merge pull request [#14](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/14) from awslabs/dependabot/npm_and_yarn/docs/elliptic-6.5.4
* Merge pull request [#13](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/13) from sliedig/develop
* Merge pull request [#12](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/12) from sliedig/develop
* Merge pull request [#11](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/11) from awslabs/dependabot/npm_and_yarn/docs/socket.io-2.4.1
* Merge pull request [#10](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/10) from awslabs/dependabot/npm_and_yarn/docs/ini-1.3.8
* Merge pull request [#9](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/9) from awslabs/dependabot/npm_and_yarn/docs/object-path-0.11.5
* Merge pull request [#7](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/7) from t1agob/develop
* Merge pull request [#8](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/8) from sliedig/develop
* Merge pull request [#5](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/5) from sliedig/develop
* Merge pull request [#4](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/4) from sliedig/develop
* Merge pull request [#3](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/3) from sliedig/develop
* Merge pull request [#2](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/2) from awslabs/dependabot/npm_and_yarn/docs/prismjs-1.21.0
* Merge pull request [#1](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/1) from sliedig/develop


[Unreleased]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.50.2...HEAD
[1.50.2]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.50.1...1.50.2
[1.50.1]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.50...1.50.1
[1.50]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.40...1.50
[1.40]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.30...1.40
[1.30]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.20...1.30
[1.20]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.19...1.20
[1.19]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.18...1.19
[1.18]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.17...1.18
[1.17]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.16...1.17
[1.16]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.15...1.16
[1.15]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.14...1.15
[1.14]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.13...1.14
[1.13]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.12...1.13
[1.12]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.11.1...1.12
[1.11.1]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.10.2...1.11.1
[1.10.2]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.11...1.10.2
[1.11]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.10.1...1.11
[1.10.1]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.10.0...1.10.1
[1.10.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.9.2...1.10.0
[1.9.2]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.9.1...1.9.2
[1.9.1]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.9.0...1.9.1
[1.9.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.8.5...1.9.0
[1.8.5]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.8.4...1.8.5
[1.8.4]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.8.3...1.8.4
[1.8.3]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.8.2...1.8.3
[1.8.2]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.8.1...1.8.2
[1.8.1]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.8.0...1.8.1
[1.8.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.7.1...1.8.0
[1.7.1]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.7.0...1.7.1
[1.7.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.6.0...1.7.0
[1.6.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.5.0...1.6.0
[1.5.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.4.2...1.5.0
[1.4.2]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.4.1...1.4.2
[1.4.1]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.4.0...1.4.1
[1.4.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.3.0...1.4.0
[1.3.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.2.0...1.3.0
[1.2.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/1.1.0...1.2.0
[1.1.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/v1.0.1...1.1.0
[v1.0.1]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/v1.0.0...v1.0.1
[v1.0.0]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/v0.0.2-preview...v1.0.0
[v0.0.2-preview]: https://github.com/aws-powertools/powertools-lambda-dotnet/compare/v0.0.1-preview.1...v0.0.2-preview

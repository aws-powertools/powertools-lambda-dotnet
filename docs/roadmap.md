---
title: Roadmap
description: Public roadmap for Powertools for AWS Lambda (.NET)
---

<!-- markdownlint-disable MD043 -->

## Overview

Our public roadmap outlines the high level direction we are working towards. We update this document when our priorities change: security and stability are our top priority.

!!! info "For most up-to-date information, see our [board of activities](https://github.com/orgs/aws-powertools/projects/6/views/14?query=is%3Aopen+sort%3Aupdated-desc){target="_blank"}."

### Key areas

Security and operational excellence take precedence above all else. This means bug fixing, stability, customer's support, and internal compliance may delay one or more key areas below.
**Missing something or want us to prioritize an existing area?**
You can help us prioritize by [upvoting existing feature requests](https://github.com/aws-powertools/powertools-lambda-dotnet/issues?q=is%3Aissue%20state%3Aopen%20label%3Afeature-request){target="_blank"}, leaving a comment on what use cases it could unblock for you, and by joining our discussions on Discord.

[![Join our Discord](https://dcbadge.vercel.app/api/server/B8zZKbbyET)](https://discord.gg/B8zZKbbyET){target="_blank"}

### Core Utilities (P0)

#### Logging V2

Modernizing our logging capabilities to align with .NET practices and improve developer experience.

- [ ] Logger buffer implementation
- [ ] New .NET-friendly API design (Serilog-like patterns)
- [ ] Filtering and JMESPath expression support
- [ ] Documentation for SDK context.Logger vs Powertools Logger differences

#### Metrics V2

Updating metrics implementation to support latest EMF specifications and improve performance.

- [ ] Update to latest EMF specifications
- [ ] Breaking changes implementation for multiple dimensions
- [ ] Add support for default dimensions on ColdStart metric
- [ ] API updates - missing functionality that is present in Python implementation (ie: flush_metrics)

### Security and Production Readiness (P1)

Ensuring enterprise-grade security and compatibility with latest .NET developments.

- [ ] .NET 10 support from day one
- [ ] Deprecation path for .NET 6
- [ ] Scorecard implementation
- [ ] Security compliance checks on our pipeline
- [ ] All utilities with end-to-end tests in our pipeline

### Feature Parity and ASP.NET Support (P2)

#### Feature Parity

Implementing key features to achieve parity with other Powertools implementations.

- [ ] Data masking
- [ ] Feature Flags
- [ ] S3 Streaming support

#### ASP.NET Support

Adding first-class support for ASP.NET Core in Lambda with performance considerations.

- [ ] AspNetCoreServer.Hosting - [Tracking issue](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/360){target="_blank"}
- [ ] Minimal APIs support
- [ ] ASP.NET Core integration
- [ ] Documentation for cold start impacts
- [ ] Clear guidance on Middleware vs. Decorators usage

#### Improve operational excellence

We continue to work on increasing operational excellence to remove as much undifferentiated heavylifting for maintainers, so that we can focus on delivering features that help you.

This means improving our automation workflows, and project management, and test coverage.

## Roadmap status definition

<center>
```mermaid
graph LR
    Ideas --> Backlog --> Work["Working on it"] --> Merged["Coming soon"] --> Shipped
```
<i>Visual representation</i>
</center>

Within our [public board](https://github.com/orgs/aws-powertools/projects/6/views/4?query=is%3Aopen+sort%3Aupdated-desc){target="_blank"}, you'll see the following values in the `Status` column:

* **Ideas**. Incoming and existing feature requests that are not being actively considered yet. These will be reviewed when bandwidth permits and based on demand.
* **Backlog**. Accepted feature requests or enhancements that we want to work on.
* **Working on it**. Features or enhancements we're currently either researching or implementing it.
* **Coming soon**. Any feature, enhancement, or bug fixes that have been merged and are coming in the next release.
* **Shipped**. Features or enhancements that are now available in the most recent release.
* **On hold**. Features or items that are currently blocked until further notice.
* **Pending review**. Features which implementation is mostly completed, but need review and some additional iterations.

> Tasks or issues with empty `Status` will be categorized in upcoming review cycles.

## Process

<center>
```mermaid
graph LR
    PFR[Feature request] --> Triage{Need RFC?}
    Triage --> |Complex/major change or new utility?| RFC[Ask or write RFC] --> Approval{Approved?}
    Triage --> |Minor feature or enhancement?| NoRFC[No RFC required] --> Approval
    Approval --> |Yes| Backlog
    Approval --> |No | Reject["Inform next steps"]
    Backlog --> |Prioritized| Implementation
    Backlog --> |Defer| WelcomeContributions["help-wanted label"]
```
<i>Visual representation</i>
</center>

Our end-to-end mechanism follows four major steps:

* **Feature Request**. Ideas start with a [feature request](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/new?assignees=&labels=feature-request%2Ctriage&projects=&template=feature_request.yml&title=Feature+request%3A+TITLE){target="_blank"} to outline their use case at a high level. For complex use cases, maintainers might ask for/write a RFC.
  * Maintainers review requests based on [project tenets](index.md#tenets){target="_blank"}, customers reaction (👍), and use cases.
* **Request-for-comments (RFC)**. Design proposals use our [RFC issue template](https://github.com/aws-powertools/powertools-lambda-dotnet/issues/new?assignees=&labels=RFC%2Ctriage&projects=&template=rfc.yml&title=RFC%3A+TITLE){target="_blank"} to describe its implementation, challenges, developer experience, dependencies, and alternative solutions.
  * This helps refine the initial idea with community feedback before a decision is made.
* **Decision**. After carefully reviewing and discussing them, maintainers make a final decision on whether to start implementation, defer or reject it, and update everyone with the next steps.
* **Implementation**. For approved features, maintainers give priority to the original authors for implementation unless it is a sensitive task that is best handled by maintainers.

??? info "See [Maintainers](https://github.com/aws-powertools/powertools-lambda-dotnet/blob/develop/MAINTAINERS.md) document to understand how we triage issues and pull requests, labels and governance."

## Disclaimer

The Powertools for AWS Lambda team values feedback and guidance from its community of users, although final decisions on inclusion into the project will be made by AWS.

We determine the high-level direction for our open roadmap based on customer feedback and popularity (👍🏽 and comments), security and operational impacts, and business value. Where features don’t meet our goals and longer-term strategy, we will communicate that clearly and openly as quickly as possible with an explanation of why the decision was made.

## FAQs

**Q: Why did you build this?**

A: We know that our customers are making decisions and plans based on what we are developing, and we want to provide our customers the insights they need to plan.

**Q: Why are there no dates on your roadmap?**

A: Because job zero is security and operational stability, we can't provide specific target dates for features. The roadmap is subject to change at any time, and roadmap issues in this repository do not guarantee a feature will be launched as proposed.

**Q: How can I provide feedback or ask for more information?**

A: For existing features, you can directly comment on issues. For anything else, please open an issue.

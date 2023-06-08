# AWS Lambda Idempotency for .NET

The idempotency package provides a simple solution to convert your Lambda functions into idempotent operations which
are safe to retry.

## Terminology

The property of idempotency means that an operation does not cause additional side effects if it is called more than
once with the same input parameters.

**Idempotent operations will return the same result when they are called multiple
times with the same parameters**. This makes idempotent operations safe to retry. [Read more](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/) about idempotency.

**Idempotency key** is a hash representation of either the entire event or a specific configured subset of the event, and invocation results are **JSON serialized** and stored in your persistence storage layer.


## Key features

* Prevent Lambda handler function from executing more than once on the same event payload during a time window
* Ensure Lambda handler returns the same result when called with the same payload
* Select a subset of the event as the idempotency key using JMESPath expressions
* Set a time window in which records with the same payload should be considered duplicates


## Installation
You should install with NuGet:

```
Install-Package Amazon.Lambda.PowerTools.Idempotency
```

Or via the .NET Core command line interface:

```
dotnet add package Amazon.Lambda.PowerTools.Idempotency
```

## Acknowledgment
This project has been ported from the Java Idempotency PowerTool Utility

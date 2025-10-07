---
title: Migration Guide v2
slug: migration-guide-v2
description: Migration guide to upgrade to v2
---

# Migration Guide to v2

This guide will help you migrate to v2

## Logging

### Breaking changes from v1 (dependency updates)

!!! info

    Looking for V1 specific documentation please go to [Logging v1](/lambda/dotnet/core/logging-v1)

| Change | Before (v1.x) | After (v2.0) | Migration Action |
|--------|---------------|--------------|-----------------|
| Amazon.Lambda.Core | 2.2.0|2.5.0 | dotnet add package Amazon.Lambda.Core |
| Amazon.Lambda.Serialization.SystemTextJson | 2.4.3 | 2.4.4 | dotnet add package Amazon.Lambda.Serialization.SystemTextJson |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | 8.0.1 | dotnet add package Microsoft.Extensions.DependencyInjection |

#### Extra keys - Breaking change

In v1.x, the extra keys were added to the log entry as a dictionary. In v2.x, the extra keys are added to the log entry as
a JSON object.

There is no longer a method that accepts extra keys as first argument.

=== "Before (v1)"

    ```csharp
    public class User
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
    
    Logger.LogInformation<User>(user, "{Name} is {Age} years old", 
          new object[]{user.Name, user.Age});
    
    var scopeKeys = new
    {
        PropOne = "Value 1",
        PropTwo = "Value 2"
    };
    Logger.LogInformation(scopeKeys, "message");
    
    ```

=== "After (v2)"

    ```csharp
    public class User
    {
        public string Name { get; set; }
        public int Age { get; set; }
        
        public override string ToString()
        {
            return $"{Name} is {Age} years old";
        }
    }

    // It uses the ToString() method of the object to log the message
    // the extra keys are added because of the {@} in the message template
    Logger.LogInformation("{@user}", user);
    
    var scopeKeys = new
    {
        PropOne = "Value 1",
        PropTwo = "Value 2"
    };

    // there is no longer a method that accepts extra keys as first argument.
    Logger.LogInformation("{@keys}", scopeKeys);
    ```

This change was made to improve the performance of the logger and to make it easier to work with the extra keys.

## Metrics

### Breaking changes from V1

!!! info

    Loooking for v1 specific documentation please go to [Metrics v1](/lambda/dotnet/core/metrics-v1)    

* **`Dimensions`** outputs as an array of arrays instead of an array of objects. Example: `Dimensions: [["service", "Environment"]]` instead of `Dimensions: ["service", "Environment"]`
* **`FunctionName`** is not added as default dimension and only to cold start metric.
* **`Default Dimensions`** can now be included in Cold Start metrics, this is a potential breaking change if you were relying on the absence of default dimensions in Cold Start metrics when searching.

<br />

<figure>
  <img src="../../media/metrics_utility_showcase.png" loading="lazy" alt="Screenshot of the Amazon CloudWatch Console showing an example of business metrics in the Metrics Explorer" />
  <figcaption>Metrics showcase - Metrics Explorer</figcaption>
</figure>


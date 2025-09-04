/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HelloWorld;

public class SamplingTestFunction
{
    [Logging(LogEvent = true, SamplingRate = 0.5)] // 50% sampling rate
    public string TestSampling(string input, ILambdaContext context)
    {
        Logger.LogInformation("Starting sampling test");
        
        // Make multiple log calls to test sampling
        for (int i = 0; i < 10; i++)
        {
            Logger.LogDebug($"Debug log #{i} - this should sometimes appear due to sampling");
            Logger.LogInformation($"Info log #{i} - this should always appear");
        }
        
        Logger.LogInformation("Sampling test completed");
        
        return $"Processed: {input}";
    }
    
    [Logging(LogEvent = true, SamplingRate = 1.0)] // 100% sampling rate
    public string TestFullSampling(string input, ILambdaContext context)
    {
        Logger.LogInformation("Starting full sampling test");
        
        // With 100% sampling, all debug logs should appear
        for (int i = 0; i < 5; i++)
        {
            Logger.LogDebug($"Debug log #{i} - should ALWAYS appear with 100% sampling");
        }
        
        Logger.LogInformation("Full sampling test completed");
        
        return $"Processed with full sampling: {input}";
    }
    
    [Logging(LogEvent = true, SamplingRate = 0.0)] // 0% sampling rate
    public string TestNoSampling(string input, ILambdaContext context)
    {
        Logger.LogInformation("Starting no sampling test");
        
        // With 0% sampling, no debug logs should appear
        for (int i = 0; i < 5; i++)
        {
            Logger.LogDebug($"Debug log #{i} - should NEVER appear with 0% sampling");
        }
        
        Logger.LogInformation("No sampling test completed");
        
        return $"Processed with no sampling: {input}";
    }
}

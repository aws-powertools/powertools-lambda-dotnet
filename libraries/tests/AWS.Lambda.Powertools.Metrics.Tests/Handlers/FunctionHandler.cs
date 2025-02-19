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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

namespace AWS.Lambda.Powertools.Metrics.Tests.Handlers;

public class FunctionHandler
{
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService", CaptureColdStart = true)]
    public void AddMetric(string name = "TestMetric", double value =1, MetricUnit  unit = MetricUnit.Count,MetricResolution resolution = MetricResolution.Default)
    {
        Metrics.AddMetric(name, value, unit, resolution);
    }

    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void AddDimensions()
    {
        Metrics.AddDimension("functionVersion", "$LATEST");
        Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "ServiceName", CaptureColdStart = true)]
    public void AddMultipleDimensions()
    {
        Metrics.PushSingleMetric("SingleMetric1", 1, MetricUnit.Count, metricResolution: MetricResolution.High,
            defaultDimensions: new Dictionary<string, string> {
                { "Default1", "SingleMetric1" }
            });
        
        Metrics.PushSingleMetric("SingleMetric2", 1, MetricUnit.Count, metricResolution: MetricResolution.High,  nameSpace: "ns2",
            defaultDimensions: new Dictionary<string, string> {
                { "Default1", "SingleMetric2" },
                { "Default2", "SingleMetric2" }
            });
        Metrics.AddMetric("AddMetric", 1, MetricUnit.Count, MetricResolution.High);
        Metrics.AddMetric("AddMetric2", 1, MetricUnit.Count, MetricResolution.High);
    }
    
    [Metrics(Namespace = "ExampleApplication")]
    public void PushSingleMetricWithNamespace()
    {
        Metrics.PushSingleMetric("SingleMetric", 1, MetricUnit.Count, metricResolution: MetricResolution.High,
            defaultDimensions: new Dictionary<string, string> {
                { "Default", "SingleMetric" }
            });
    }
    
    [Metrics]
    public void PushSingleMetricWithEnvNamespace()
    {
        Metrics.PushSingleMetric("SingleMetric", 1, MetricUnit.Count, metricResolution: MetricResolution.High,
            defaultDimensions: new Dictionary<string, string> {
                { "Default", "SingleMetric" }
            });
    }

    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void ClearDimensions()
    {
        Metrics.ClearDefaultDimensions();
        Metrics.AddMetric("Metric Name", 1, MetricUnit.Count);
    }

    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void MaxMetrics(int maxMetrics)
    {
        for (var i = 0; i <= maxMetrics; i++)
        {
            Metrics.AddMetric($"Metric Name {i + 1}", i, MetricUnit.Count);
        }
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void MaxMetricsSameName(int maxMetrics)
    {
        for (var i = 0; i <= maxMetrics; i++)
        {
            Metrics.AddMetric("Metric Name", i, MetricUnit.Count);
        }
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void MaxDimensions(int maxDimensions)
    {
        for (var i = 0; i <= maxDimensions; i++)
        {
            Metrics.AddDimension($"Dimension Name {i + 1}", $"Dimension Value {i + 1}");
        }
    }
    
    [Metrics(Service = "testService")]
    public void NoNamespace()
    {
        Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void AddMetadata()
    {
        Metrics.AddMetadata("test_metadata", "test_value");
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void AddDefaultDimensions(Dictionary<string, string> defaultDimensions)
    {
        Metrics.SetDefaultDimensions(defaultDimensions);
        Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void AddDefaultDimensionsTwice(Dictionary<string, string> defaultDimensions)
    {
        Metrics.SetDefaultDimensions(defaultDimensions);
        Metrics.SetDefaultDimensions(defaultDimensions);
        Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void AddDimensionMetricMetadata(string metricKey, string metadataKey)
    {
        Metrics.AddDimension("functionVersion", "$LATEST");
        Metrics.AddMetric(metricKey, 100.7, MetricUnit.Milliseconds);
        Metrics.AddMetadata(metadataKey, "dev");
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public void AddMetricSameName()
    {
        Metrics.AddDimension("functionVersion", "$LATEST");
        Metrics.AddMetric("Time", 100.5, MetricUnit.Milliseconds);
        Metrics.AddMetric("Time", 200, MetricUnit.Milliseconds);
    }
    
    [Metrics(Namespace = "dotnet-powertools-test", Service = "testService")]
    public async Task RaceConditon()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => { Metrics.AddMetric($"Metric Name", 0, MetricUnit.Count); }));
        }

        await Task.WhenAll(tasks);
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task<string> HandleSameKey(string input)
    {
        Metrics.AddMetric("MyMetric", 1);
        Metrics.AddMetadata("MyMetric", "meta");

        await Task.Delay(1);

        return input.ToUpper(CultureInfo.InvariantCulture);
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task<string> HandleTestSecondCall(string input)
    {
        Metrics.AddMetric("MyMetric", 1);
        Metrics.AddMetadata("MyMetadata", "meta");

        await Task.Delay(1);

        return input.ToUpper(CultureInfo.InvariantCulture);
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    public async Task<string> HandleMultipleThreads(string input)
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Environment.ProcessorCount * 2), async (x, _) =>
        {
            Metrics.AddMetric("MyMetric", 1);
            await Task.Delay(1);
        });

        return input.ToUpper(CultureInfo.InvariantCulture);
    }

    [Metrics(Namespace = "ns", Service = "svc", CaptureColdStart = true)]
    public void HandleWithLambdaContext(ILambdaContext context)
    {
        
    }

    [Metrics(Namespace = "ns", Service = "svc")]
    public void HandleColdStartNoContext()
    {
        Metrics.AddMetric("MyMetric", 1);
    }
    
    [Metrics(Namespace = "ns", Service = "svc", CaptureColdStart = true)]
    public void HandleWithParamAndLambdaContext(string input, ILambdaContext context)
    {
        
    }
}
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
using System.IO;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common;
using Moq;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.Powertools.Metrics.Tests
{
    [Collection("Sequential")]
    public class EmfValidationTests
    {
        [Trait("Category", value: "SchemaValidation")]
        [Fact]
        public void WhenCaptureColdStart_CreateSeparateBlob()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            const bool captureColdStartEnabled = true;
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService",
                captureColdStartEnabled: captureColdStartEnabled
            );

            var handler = new MetricsAspectHandler(
                metrics,
                captureColdStartEnabled
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
            handler.OnExit(eventArgs);

            var metricsOutput = consoleOut.ToString();

            // Assert
            var metricBlobs = AllIndexesOf(metricsOutput, "_aws");

            Assert.Equal(2, metricBlobs.Count);

            // Reset
            handler.ResetForTest();
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenCaptureColdStartEnabled_ValidateExists()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            const bool captureColdStartEnabled = true;
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();

            var logger = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService",
                captureColdStartEnabled: captureColdStartEnabled
            );

            var handler = new MetricsAspectHandler(
                logger,
                captureColdStartEnabled
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}]", result);
            Assert.Contains("\"ColdStart\":1", result);

            handler.ResetForTest();
        }

        [Trait("Category", "EMFLimits")]
        [Fact]
        public void When100MetricsAreAdded_FlushAutomatically()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            for (var i = 0; i <= 100; i++)
            {
                Metrics.AddMetric($"Metric Name {i + 1}", i, MetricUnit.Count);
            }

            handler.OnExit(eventArgs);

            var metricsOutput = consoleOut.ToString();

            // Assert
            Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 101\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]", metricsOutput);

            // Reset
            handler.ResetForTest();
        }



        [Trait("Category", "EMFLimits")]
        [Fact]
        public void WhenMoreThan9DimensionsAdded_ThrowArgumentOutOfRangeException()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            var act = () =>
            {
                for (var i = 0; i <= 9; i++)
                {
                    Metrics.AddDimension($"Dimension Name {i + 1}", $"Dimension Value {i + 1}");
                }
            };

            handler.OnExit(eventArgs);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(act);

            // Reset 
            handler.ResetForTest();
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenNamespaceNotDefined_ThrowSchemaValidationException()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            var act = () =>
            {
                handler.OnEntry(eventArgs);
                Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
                handler.OnExit(eventArgs);
            };

            // Assert
            var exception = Assert.Throws<SchemaValidationException>(act);
            Assert.Equal("EMF schema is invalid. 'namespace' is mandatory and not specified.", exception.Message);

            // RESET
            handler.ResetForTest();
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenDimensionsAreAdded_MustExistAsMembers()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.AddDimension("functionVersion", "$LATEST");
            Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"],[\"functionVersion\"]]"
                , result);
            Assert.Contains("\"Service\":\"testService\",\"functionVersion\":\"$LATEST\""
                , result);

            // Reset
            handler.ResetForTest();
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenNamespaceIsDefined_AbleToRetrieveNamespace()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();
            var metrics = new Metrics(configurations.Object);

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.SetNamespace("dotnet-powertools-test");

            var result = Metrics.GetNamespace();

            // Assert
            Assert.Equal("dotnet-powertools-test", result);

            // Reset
            handler.ResetForTest();
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsDefined_AbleToAddMetadata()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();
            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.AddMetadata("test_metadata", "test_value");
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"test_metadata\":\"test_value\"", result);

            // Reset
            handler.ResetForTest();
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenDefaultDimensionsSet_ValidInitialization()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var defaultDimensions = new Dictionary<string, string> { { "CustomDefaultDimension", "CustomDefaultDimensionValue" } };
            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.SetDefaultDimensions(defaultDimensions);
            Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"],[\"CustomDefaultDimension\"]", result);
            Assert.Contains("\"CustomDefaultDimension\":\"CustomDefaultDimensionValue\"", result);

            // Reset
            handler.ResetForTest();
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricIsNegativeValue_ThrowException()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            var act = () =>
            {
                const int metricValue = -1;
                handler.OnEntry(eventArgs);
                Metrics.AddMetric("TestMetric", metricValue, MetricUnit.Count);
                handler.OnExit(eventArgs);
            };

            // Assert
            var exception = Assert.Throws<ArgumentException>(act);
            Assert.Equal("'AddMetric' method requires a valid metrics value. Value must be >= 0.", exception.Message);

            // RESET
            handler.ResetForTest();
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenDefaultDimensionSet_IgnoreDuplicates()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);
            var configurations = new Mock<IPowertoolsConfigurations>();
            var defaultDimensions = new Dictionary<string, string> { { "CustomDefaultDimension", "CustomDefaultDimensionValue" } };

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.SetDefaultDimensions(defaultDimensions);
            Metrics.SetDefaultDimensions(defaultDimensions);
            Metrics.AddMetric("TestMetric", 1, MetricUnit.Count);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"],[\"CustomDefaultDimension\"]", result);
            Assert.Contains("\"CustomDefaultDimension\":\"CustomDefaultDimensionValue\"", result);

            // Reset
            handler.ResetForTest();
        }

        [Fact]
        public void WhenMetricsAndMetadataAdded_ValidateOutput()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);
            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act 
            handler.OnEntry(eventArgs);
            Metrics.AddDimension("functionVersion", "$LATEST");
            Metrics.AddMetric("Time", 100.7, MetricUnit.Milliseconds);
            Metrics.AddMetadata("env", "dev");
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}],\"Dimensions\":[[\"Service\"],[\"functionVersion\"]]}]},\"Service\":\"testService\",\"functionVersion\":\"$LATEST\",\"env\":\"dev\",\"Time\":100.7}"
                , result);

            // Reset
            handler.ResetForTest();
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsWithSameNameAdded_ValidateMetricArray()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act 
            handler.OnEntry(eventArgs);
            Metrics.AddDimension("functionVersion", "$LATEST");
            Metrics.AddMetric("Time", 100.5, MetricUnit.Milliseconds);
            Metrics.AddMetric("Time", 200, MetricUnit.Milliseconds);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}]"
                , result);
            Assert.Contains("\"Time\":[100.5,200]"
                , result);

            // Reset
            handler.ResetForTest();
        }
        
        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsWithStandardResolutionAdded_ValidateMetricArray()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act 
            handler.OnEntry(eventArgs);
            Metrics.AddDimension("functionVersion", "$LATEST");
            Metrics.AddMetric("Time", 100.5, MetricUnit.Milliseconds, MetricResolution.Standard);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\",\"StorageResolution\":60}]"
                , result);
            Assert.Contains("\"Time\":100.5"
                , result);

            // Reset
            handler.ResetForTest();
        }
        
        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsWithHighResolutionAdded_ValidateMetricArray()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(
                configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService"
            );

            var handler = new MetricsAspectHandler(
                metrics,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act 
            handler.OnEntry(eventArgs);
            Metrics.AddDimension("functionVersion", "$LATEST");
            Metrics.AddMetric("Time", 100.5, MetricUnit.Milliseconds, MetricResolution.High);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\",\"StorageResolution\":1}]"
                , result);
            Assert.Contains("\"Time\":100.5"
                , result);

            // Reset
            handler.ResetForTest();
        }

        #region Helpers

        private List<int> AllIndexesOf(string str, string value)
        {
            var indexes = new List<int>();

            if (string.IsNullOrEmpty(value)) return indexes;

            for (var index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index, StringComparison.Ordinal);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        #endregion
        
        [Fact]
        public async Task WhenMetricsAsyncRaceConditionItemSameKeyExists_ValidateLock()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowertoolsConfigurations>();

            var metrics = new Metrics(configurations.Object,
                nameSpace: "dotnet-powertools-test",
                service: "testService");

            var handler = new MetricsAspectHandler(metrics,
                false);

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            var tasks = new List<Task>();
            for (var i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    Metrics.AddMetric($"Metric Name", 0, MetricUnit.Count);
                }));
            }

            await Task.WhenAll(tasks);


            handler.OnExit(eventArgs);

            var metricsOutput = consoleOut.ToString();

            // Assert
            Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]]",
                metricsOutput);

            // Reset
            handler.ResetForTest();
        }
    }
}

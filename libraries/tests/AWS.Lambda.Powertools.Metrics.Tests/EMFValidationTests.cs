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

                if (i == 100)
                {
                    // flush when it reaches 100 items
                    Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 1\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 2\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 3\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 4\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 5\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 6\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 7\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 8\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 9\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 10\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 11\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 12\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 13\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 14\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 15\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 16\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 17\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 18\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 19\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 20\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 21\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 22\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 23\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 24\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 25\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 26\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 27\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 28\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 29\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 30\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 31\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 32\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 33\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 34\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 35\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 36\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 37\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 38\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 39\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 40\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 41\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 42\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 43\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 44\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 45\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 46\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 47\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 48\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 49\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 50\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 51\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 52\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 53\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 54\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 55\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 56\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 57\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 58\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 59\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 60\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 61\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 62\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 63\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 64\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 65\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 66\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 67\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 68\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 69\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 70\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 71\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 72\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 73\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 74\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 75\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 76\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 77\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 78\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 79\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 80\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 81\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 82\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 83\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 84\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 85\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 86\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 87\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 88\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 89\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 90\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 91\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 92\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 93\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 94\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 95\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 96\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 97\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 98\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 99\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 100\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]]", consoleOut.ToString());
                }
            }
            handler.OnExit(eventArgs);

            var metricsOutput = consoleOut.ToString();

            // Assert
            // flush the 101 item only
            Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 101\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]", metricsOutput);

            // Reset
            handler.ResetForTest();
        }

        [Trait("Category", "EMFLimits")]
        [Fact]
        public void When100DataPointsAreAddedToTheSameMetric_FlushAutomatically()
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
                Metrics.AddMetric($"Metric Name", i, MetricUnit.Count);
                if(i == 100)
                {
                    // flush when it reaches 100 items
                    Assert.Contains(
                        "\"Service\":\"testService\",\"Metric Name\":[0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99]}",
                        consoleOut.ToString());
                }
            }

            handler.OnExit(eventArgs);

            var metricsOutput = consoleOut.ToString();

            // Assert
            // flush the 101 item only
            Assert.Contains("[[\"Service\"]]}]},\"Service\":\"testService\",\"Metric Name\":100}", metricsOutput);

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
    }
}

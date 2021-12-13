using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using AWS.Lambda.PowerTools.Core;
using AWS.Lambda.PowerTools.Metrics.Internal;
using AWS.Lambda.PowerTools.Aspects;
using System.IO;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.PowerTools.Metrics.Tests
{
    public class EmfValidationTests
    {
        [Trait("Category", "EMFLimits")]
        [Fact]
        public void When100MetricsAreAdded_FlushAutomatically()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);
            
            var configurations = new Mock<IPowerToolsConfigurations>();

            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );

            var handler = new MetricsAspectHandler(
                logger,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            for (int i = 0; i <= 100; i++)
            {
                Metrics.AddMetric($"Metric Name {i + 1}", i, MetricUnit.COUNT);
            }

            handler.OnExit(eventArgs);

            var metricsOutput = consoleOut.ToString();

            // Assert
            Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 101\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]", metricsOutput);

            // Reset
            Metrics.ResetForTesting();
        }

        [Trait("Category", value: "SchemaValidation")]
        [Fact]
        public void WhenCaptureColdStart_CreateSeparateBlob()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);
            
            var configurations = new Mock<IPowerToolsConfigurations>();

            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );

            var handler = new MetricsAspectHandler(
                logger,
                true
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.AddMetric("TestMetric", 1, MetricUnit.COUNT);
            handler.OnExit(eventArgs);

            var metricsOutput = consoleOut.ToString();

            // Assert
            var metricBlobs = AllIndexesOf(metricsOutput.ToString(), "_aws");
            
            Assert.Equal(2, metricBlobs.Count);

            // Reset
            Metrics.ResetForTesting();
        }
        
        [Trait("Category", "EMFLimits")]
        [Fact]
        public void WhenMoreThan9DimensionsAdded_ThrowArgumentOutOfRangeException()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            
            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );
            
            var handler = new MetricsAspectHandler(
                logger,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);

            Action act = () =>
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
            Metrics.ResetForTesting();
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenNamespaceNotDefined_ThrowSchemaValidationException()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            
            var logger = new Metrics(
                configurations.Object
            );
            
            var handler = new MetricsAspectHandler(
                logger,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            Action act = () =>{
                handler.OnEntry(eventArgs);
                Metrics.AddMetric("TestMetric", 1, MetricUnit.COUNT);
                handler.OnExit(eventArgs);
            };

            // Assert
            var exception = Assert.Throws<SchemaValidationException>(act);
            Assert.Equal("EMF schema is invalid. 'namespace' is mandatory and not specified.", exception.Message);

            // RESET
            Metrics.ResetForTesting();
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenDimensionsAreAdded_MustExistAsMembers()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);
            
            var configurations = new Mock<IPowerToolsConfigurations>();

            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );

            var handler = new MetricsAspectHandler(
                logger,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.AddDimension("functionVersion", "$LATEST");
            Metrics.AddMetric("TestMetric", 1, MetricUnit.COUNT);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"],[\"functionVersion\"]]"
                , result);
            Assert.Contains("\"Service\":\"testService\",\"functionVersion\":\"$LATEST\""
                , result);

            // Reset
            Metrics.ResetForTesting();
        }        

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenCaptureColdStartEnabled_ValidateExists()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowerToolsConfigurations>();
            
            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );
            
            var handler = new MetricsAspectHandler(
                logger,
                true
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.AddMetric("TestMetric", 1, MetricUnit.COUNT);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}]", result);
            Assert.Contains("\"ColdStart\":1", result);

            Metrics.ResetForTesting();
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenNamespaceIsDefined_AbleToRetrieveNamespace()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var configurations = new Mock<IPowerToolsConfigurations>();
            var logger = new Metrics(configurations.Object);

            var handler = new MetricsAspectHandler(
                logger,
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
            Metrics.ResetForTesting();
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsDefined_AbleToAddMetadata()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowerToolsConfigurations>();
            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );

            var handler = new MetricsAspectHandler(
                logger,
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
            Metrics.ResetForTesting();
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
            var configurations = new Mock<IPowerToolsConfigurations>();
            
            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );

            var handler = new MetricsAspectHandler(
                logger,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.SetDefaultDimensions(defaultDimensions);
            Metrics.AddMetric("TestMetric", 1, MetricUnit.COUNT);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"],[\"CustomDefaultDimension\"]", result);
            Assert.Contains("\"CustomDefaultDimension\":\"CustomDefaultDimensionValue\"", result);

            // Reset
            Metrics.ResetForTesting();
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenDefaultDimensionSet_IgnoreDuplicates()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);
            var configurations = new Mock<IPowerToolsConfigurations>();
            var defaultDimensions = new Dictionary<string, string> { { "CustomDefaultDimension", "CustomDefaultDimensionValue" } };
            
            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );
            
            var handler = new MetricsAspectHandler(
                logger,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act
            handler.OnEntry(eventArgs);
            Metrics.SetDefaultDimensions(defaultDimensions);
            Metrics.SetDefaultDimensions(defaultDimensions);
            Metrics.AddMetric("TestMetric", 1, MetricUnit.COUNT);
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"],[\"CustomDefaultDimension\"]", result);
            Assert.Contains("\"CustomDefaultDimension\":\"CustomDefaultDimensionValue\"", result);

            // Reset
            Metrics.ResetForTesting();
        }

        [Fact]
        public void WhenMetricsAndMetadataAdded_ValidateOutput()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);
            var configurations = new Mock<IPowerToolsConfigurations>();

            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );

            var handler = new MetricsAspectHandler(
                logger,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act 
            handler.OnEntry(eventArgs);
            Metrics.AddDimension("functionVersion", "$LATEST");
            Metrics.AddMetric("Time", 100.7, MetricUnit.MILLISECONDS);
            Metrics.AddMetadata("env", "dev");
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}],\"Dimensions\":[[\"Service\"],[\"functionVersion\"]]}]},\"Service\":\"testService\",\"functionVersion\":\"$LATEST\",\"env\":\"dev\",\"Time\":100.7}"
                , result);

            // Reset
            Metrics.ResetForTesting();
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsWithSameNameAdded_ValidateMetricArray()
        {
            // Arrange
            var methodName = Guid.NewGuid().ToString();
            var consoleOut = new StringWriter();
            Console.SetOut(consoleOut);

            var configurations = new Mock<IPowerToolsConfigurations>();
            
            var logger = new Metrics(
                configurations.Object,
                metricsNamespace: "dotnet-powertools-test",
                serviceName: "testService"
            );

            var handler = new MetricsAspectHandler(
                logger,
                false
            );

            var eventArgs = new AspectEventArgs { Name = methodName };

            // Act 
            handler.OnEntry(eventArgs);
            Metrics.AddDimension("functionVersion", "$LATEST");
            Metrics.AddMetric("Time", 100.5, MetricUnit.MILLISECONDS);
            Metrics.AddMetric("Time", 200, MetricUnit.MILLISECONDS);            
            handler.OnExit(eventArgs);

            var result = consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}]"
                , result);
            Assert.Contains("\"Time\":[100.5,200]"
                , result);

            // Reset
            Metrics.ResetForTesting();
        }

        #region Helpers

        public List<int> AllIndexesOf(string str, string value)
        {
            var indexes = new List<int>();

            if (string.IsNullOrEmpty(value)) return indexes;
            
            for (var index = 0;; index += value.Length)
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

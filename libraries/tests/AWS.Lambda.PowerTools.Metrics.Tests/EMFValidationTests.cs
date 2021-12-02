using System;
using System.Collections.Generic;
using Xunit;

namespace AWS.Lambda.PowerTools.Metrics.Tests
{
    public class EMFValidationTests
    {
        [Fact]
        public void FlushesAfter100Metrics()
        {
            // Arrange
            Metrics logger = new Metrics("dotnet-powertools-test", "testService");
            for (int i = 0; i <= 100; i++)
            {
                logger.AddMetric($"Metric Name {i + 1}", i, MetricUnit.COUNT);
            }

            // Act
            var metricsOutput = logger.Serialize();

            // Assert
            Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 101\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]]}", metricsOutput);
        }

        [Fact]
        public void CannotAddMoreThan9Dimensions()
        {
            // Arrange
            Metrics logger = new Metrics("dotnet-powertools-test", "testService");

            // Act
            Action act = () =>
            {
                for (int i = 0; i <= 9; i++)
                {
                    logger.AddDimension($"Dimension Name {i + 1}", $"Dimension Value {i + 1}");
                }
            };

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void SingleMetricSupportsMoreThanOneValue()
        {
            // Arrange
            MetricsContext context = new MetricsContext();
            context.SetNamespace("dotnet-powertools-test");
            context.AddDimension("functionVersion", "$LATEST");
            context.AddMetric("Time", 100, MetricUnit.MILLISECONDS);
            context.AddMetric("Time", 200, MetricUnit.MILLISECONDS);

            // Act
            var metrics = context.GetMetrics();

            // Assert
            Assert.Single(metrics);
            Assert.Equal(2, metrics[0].Values.Count);
        }

        [Fact]
        public void ValidateEMFWithDimensionMetricAndMetadata()
        {
            // Arrange
            MetricsContext context = new MetricsContext();
            context.SetNamespace("dotnet-powertools-test");
            context.AddDimension("functionVersion", "$LATEST");
            context.AddMetric("Time", 100, MetricUnit.MILLISECONDS);
            context.AddMetadata("env", "dev");

            // Act 
            string result = context.Serialize();

            // Assert
            Assert.Contains("CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}],\"Dimensions\":[[\"functionVersion\"]]}]},\"functionVersion\":\"$LATEST\",\"env\":\"dev\",\"Time\":100.0}"
                , result);
        }

        [Fact]
        public void ThrowOnSerializationWithoutNamespace()
        {
            // Arrange
            Metrics logger = new Metrics(false);
            logger.AddMetric("Time", 100, MetricUnit.MILLISECONDS);
       
            // Act
            Action act = () => logger.Serialize();

            // Assert
            SchemaValidationException exception = Assert.Throws<SchemaValidationException>(act);

            Assert.Equal("EMF schema is invalid. 'namespace' is mandatory and not specified.", exception.Message);
        }

        [Fact]
        public void DimensionsMustExistAsMembers()
        {
            // Arrange
            Metrics logger = new Metrics("dotnet-powertools-test", "testService", false);
            logger.AddDimension("functionVersion", "$LATEST");

            // Act
            string result = logger.Serialize();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"],[\"functionVersion\"]]"
                , result);
            Assert.Contains("\"Service\":\"testService\",\"functionVersion\":\"$LATEST\""
                , result);
        }  

        [Fact]
        public void ThrowOnMetricsWithoutParametersOrEnvVariables(){
            // Arrange
            Metrics logger = new Metrics();

            // Act
            Action act = () => logger.Serialize();

            // Assert
            SchemaValidationException exception = Assert.Throws<SchemaValidationException>(act);

            Assert.Equal("EMF schema is invalid. 'namespace' is mandatory and not specified.", exception.Message);
        }

        [Fact]
        public void CaptureColdStartOnSerialize(){
            // Arrange
            Metrics logger = new Metrics("dotnet-powertools-test", "testService", true);

            // Act
            string result = logger.Serialize();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}]", result);
            Assert.Contains("\"ColdStart\":1.0", result);
        }

        [Fact]
        public void SetAndGetMetricsNamespace(){
            // Arrange
            Metrics logger = new Metrics();
            logger.SetNamespace("dotnet-powertools-test");

            // Act
            string result = logger.GetNamespace();

            // Assert
            Assert.Equal("dotnet-powertools-test", result);
        }

        [Fact]
        public void AbleToAddMetadata(){
            // Arrange
            Metrics logger = new Metrics("dotnet-powertools-test", "testService");
            logger.AddMetadata("test_metadata", "test_value");

            // Act
            string result = logger.Serialize();

            // Assert
            Assert.Contains("\"test_metadata\":\"test_value\"", result);
        }

        [Fact]
        public void ValidInitializationWithDefaultDimensions(){
            // Arrange
            Metrics logger = new Metrics("dotnet-powertools-test", "testService")
                            .WithDefaultDimensions(new Dictionary<string, string>
                            {
                                {"CustomDefaultDimension", "CustomDefaultDimensionValue"}
                            });
            // Act
            string result = logger.Serialize();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"CustomDefaultDimension\"]", result);
            Assert.Contains("\"CustomDefaultDimension\":\"CustomDefaultDimensionValue\"", result);
        }
    }
}

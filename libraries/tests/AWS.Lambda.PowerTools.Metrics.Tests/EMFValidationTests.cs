using System;
using AWS.Lambda.PowerTools.Metrics.Model;
using Xunit;

namespace AWS.Lambda.PowerTools.Metrics.Tests
{
    public class EMFValidationTests
    {
        [Fact]
        public void FlushesAfter100Metrics()
        {
            // Initialize
            Metrics logger = new Metrics("dotnet-powertools-test", "testService");
            for (int i = 0; i <= 100; i++)
            {
                logger.AddMetric($"Metric Name {i + 1}", i, MetricUnit.COUNT);
            }

            // Execute
            var metricsOutput = logger.Serialize();

            // Assert
            Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 101\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]]}", metricsOutput);
        }

        [Fact]
        public void CannotAddMoreThan9Dimensions()
        {
            // Initialize
            Metrics logger = new Metrics("dotnet-powertools-test", "testService");

            // Execute & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                for (int i = 0; i <= 9; i++)
                {
                    logger.AddDimension($"Dimension Name {i + 1}", $"Dimension Value {i + 1}");
                }
            });
        }

        [Fact]
        public void SingleMetricSupportsMoreThanOneValue()
        {
            // Initialize
            MetricsContext context = new MetricsContext();
            context.SetNamespace("dotnet-powertools-test");
            context.AddDimension("functionVersion", "$LATEST");
            context.AddMetric("Time", 100, MetricUnit.MILLISECONDS);
            context.AddMetric("Time", 200, MetricUnit.MILLISECONDS);

            // Execute
            var metrics = context.GetMetrics();

            // Assert
            Assert.Single(metrics);
            Assert.Equal(2, metrics[0].Values.Count);
        }

        [Fact]
        public void ValidateEMFWithDimensionMetricAndMetadata()
        {
            // Initialize
            MetricsContext context = new MetricsContext();
            context.SetNamespace("dotnet-powertools-test");
            context.AddDimension("functionVersion", "$LATEST");
            context.AddMetric("Time", 100, MetricUnit.MILLISECONDS);
            context.AddMetadata("env", "dev");

            // Execute 
            string result = context.Serialize();

            // Assert
            Assert.Contains("CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}],\"Dimensions\":[[\"functionVersion\"]]}]},\"functionVersion\":\"$LATEST\",\"env\":\"dev\",\"Time\":100.0}"
                , result);
        }

        [Fact]
        public void ThrowOnSerializationWithoutNamespace()
        {
            // Initialize
            Metrics logger = new Metrics(false);
            logger.AddMetric("Time", 100, MetricUnit.MILLISECONDS);
       

            // Execute & Assert
            Assert.Throws<ArgumentNullException>("namespace", () =>
            {
                var res = logger.Serialize();
            });
        }

        [Fact]
        public void DimensionsMustExistAsMembers()
        {
            // Initialize
            Metrics logger = new Metrics("dotnet-powertools-test", "testService", false);
            logger.AddDimension("functionVersion", "$LATEST");

            // Execute
            string result = logger.Serialize();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"],[\"functionVersion\"]]"
                , result);
            Assert.Contains("\"Service\":\"testService\",\"functionVersion\":\"$LATEST\""
                , result);
        }  
    }
}

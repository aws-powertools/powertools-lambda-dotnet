using System;
using Amazon.LambdaPowertools.Metrics.Model;
using Xunit;

namespace Amazon.LambdaPowertools.Metrics.Tests
{
    public class EMFValidationTests
    {
        [Fact]
        public void FlushesAfter100Metrics()
        {
            // Initialize
            MetricsLogger logger = new MetricsLogger("dotnet-powertools-test", "testService");
            for (int i = 0; i <= 100; i++)
            {
                logger.AddMetric($"Metric Name {i + 1}", i, MetricsUnit.COUNT);
            }

            // Execute
            var metricsOutput = logger.Serialize();

            // Assert
            Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 101\",\"Unit\":\"COUNT\"}],\"Dimensions\":[[\"Service\"]]}", metricsOutput);
        }

        [Fact]
        public void CannotAddMoreThan9Dimensions()
        {
            // Initialize
            MetricsLogger logger = new MetricsLogger("dotnet-powertools-test", "testService");

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
            context.AddMetric("Time", 100, MetricsUnit.MILLISECONDS);
            context.AddMetric("Time", 200, MetricsUnit.MILLISECONDS);

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
            context.AddMetric("Time", 100, MetricsUnit.MILLISECONDS);
            context.AddMetadata("env", "dev");

            // Execute 
            string result = context.Serialize();

            // Assert
            Assert.Contains("CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"MILLISECONDS\"}],\"Dimensions\":[[\"functionVersion\"]]}]},\"functionVersion\":\"$LATEST\",\"env\":\"dev\",\"Time\":100}"
                , result);
        }

        [Fact]
        public void ThrowOnSerializationWithoutNamespace()
        {
            // Initialize
            MetricsLogger logger = new MetricsLogger(false);
            logger.AddMetric("Time", 100, MetricsUnit.MILLISECONDS);
       

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
            MetricsLogger logger = new MetricsLogger("dotnet-powertools-test", "testService", false);
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

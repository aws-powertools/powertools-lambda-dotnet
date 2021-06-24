using System;
using Amazon.LambdaPowertools.Metrics.Model;
using Xunit;

namespace Amazon.LambdaPowertools.Metrics.Tests
{
    public class EMFValidationTests
    {
        // LOGGER TESTS


        // ROOT NODE TESTS


        // METRIC DIRECTIVE TESTS


        // METRIC DEFINITION TESTS

        [Fact]
        public void FlushesAfter100Metrics()
        {
            // Initialize
            MetricsLogger logger = new MetricsLogger("dotnet-powertools-test", "testService");
            for (int i = 0; i <= 100; i++)
            {
                logger.AddMetric($"Metric Name {i + 1}", i, Unit.COUNT);
            }

            var metricsOutput = logger.Serialize();

            // Assert
            Assert.Contains("{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 101\",\"Unit\":\"COUNT\"}],\"Dimensions\":[[\"ServiceName\"]]}", metricsOutput);
        }

        [Fact]
        public void CannotAddMoreThan9Dimensions()
        {
            // Initialize
            MetricsLogger logger = new MetricsLogger("dotnet-powertools-test", "testService");

            // Assert
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
            context.AddMetric("Time", 100, Unit.MILLISECONDS);
            context.AddMetric("Time", 200, Unit.MILLISECONDS);

            // Assert
            var metrics = context.GetMetrics();
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
            context.AddMetric("Time", 100, Unit.MILLISECONDS);
            context.AddMetadata("env", "dev");

            // Assert
            string result = context.Serialize();
            Assert.Contains("CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"MILLISECONDS\"}],\"Dimensions\":[[\"functionVersion\"]]}]},\"functionVersion\":\"$LATEST\",\"env\":\"dev\",\"Time\":100}"
                , result);
        }

        [Fact]
        public void ThrowOnSerializationWithoutNamespace()
        {
            // Initialize
            MetricsLogger logger = new MetricsLogger(false);
            logger.AddMetric("Time", 100, Unit.MILLISECONDS);
       

            // Assert
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

            // Assert
            string result = logger.Serialize();
            Assert.Contains("\"Dimensions\":[[\"ServiceName\"],[\"functionVersion\"]]"
                , result);
            Assert.Contains("\"ServiceName\":\"testService\",\"functionVersion\":\"$LATEST\""
                , result);
        }

        
    }
}

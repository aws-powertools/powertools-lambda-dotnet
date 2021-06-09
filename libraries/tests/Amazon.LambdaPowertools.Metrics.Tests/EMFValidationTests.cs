using System;
using Amazon.LambdaPowertools.Metrics.Model;
using Xunit;

namespace Amazon.LambdaPowertools.Metrics.Tests
{
    public class EMFValidationTests
    {
        [Fact]
        public void CannotAddMoreThan100Metrics()
        {
            // Initialize
            MetricsContext context = new MetricsContext();


            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                for (int i = 0; i <= 100; i++)
                {
                    Console.WriteLine($"Adding Metric #{i + 1}");
                    context.AddMetric($"Metric Name {i + 1}", i, Unit.COUNT);
                }
            });
        }

        [Fact]
        public void CannotAddMoreThan9Dimensions()
        {
            // Initialize
            MetricsContext context = new MetricsContext();
            

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                for (int i = 0; i <= 9; i++)
                {
                    Console.WriteLine($"Adding Dimension #{i + 1}");
                    context.AddDimension($"Dimension Name {i + 1}", $"Dimension Value {i + 1}");
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
    }
}

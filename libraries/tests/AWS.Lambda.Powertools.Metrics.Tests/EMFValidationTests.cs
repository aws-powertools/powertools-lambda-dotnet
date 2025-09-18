using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Metrics.Tests.Handlers;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AWS.Lambda.Powertools.Metrics.Tests
{
    [Collection("Sequential")]
    public class EmfValidationTests : IDisposable
    {
        private readonly CustomConsoleWriter _consoleOut;
        private readonly FunctionHandler _handler;

        public EmfValidationTests()
        {
            _handler = new FunctionHandler();
            _consoleOut = new CustomConsoleWriter();
            ConsoleWrapper.SetOut(_consoleOut);
        }

        [Trait("Category", value: "SchemaValidation")]
        [Fact]
        public void WhenCaptureColdStart_CreateSeparateBlob()
        {
            // Act
            _handler.AddMetric();

            var metricsOutput = _consoleOut.ToString();

            // Assert
            var metricBlobs = AllIndexesOf(metricsOutput, "_aws");
            Assert.Equal(2, metricBlobs.Count);
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenCaptureColdStartEnabled_ValidateExists()
        {
            // Act
            _handler.AddMetric();

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}]", result);
            Assert.Contains("\"ColdStart\":1", result);
        }

        [Trait("Category", "EMFLimits")]
        [Fact]
        public void WhenMaxMetricsAreAdded_FlushAutomatically()
        {
            // Act
            _handler.MaxMetrics(PowertoolsConfigurations.MaxMetrics);

            var metricsOutput = _consoleOut.OutputValues;

            // Assert

            // flush when it reaches MaxMetrics
            Assert.Contains(
                "{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 1\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 2\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 3\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 4\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 5\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 6\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 7\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 8\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 9\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 10\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 11\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 12\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 13\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 14\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 15\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 16\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 17\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 18\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 19\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 20\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 21\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 22\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 23\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 24\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 25\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 26\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 27\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 28\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 29\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 30\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 31\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 32\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 33\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 34\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 35\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 36\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 37\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 38\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 39\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 40\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 41\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 42\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 43\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 44\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 45\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 46\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 47\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 48\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 49\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 50\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 51\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 52\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 53\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 54\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 55\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 56\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 57\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 58\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 59\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 60\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 61\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 62\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 63\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 64\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 65\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 66\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 67\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 68\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 69\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 70\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 71\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 72\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 73\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 74\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 75\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 76\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 77\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 78\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 79\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 80\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 81\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 82\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 83\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 84\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 85\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 86\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 87\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 88\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 89\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 90\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 91\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 92\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 93\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 94\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 95\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 96\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 97\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 98\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 99\",\"Unit\":\"Count\"},{\"Name\":\"Metric Name 100\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]]",
                metricsOutput[0]);

            // flush the (MaxMetrics + 1) item only
            Assert.Contains(
                "{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name 101\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]]",
                metricsOutput[1]);
        }

        [Trait("Category", "EMFLimits")]
        [Fact]
        public void WhenMaxDataPointsAreAddedToTheSameMetric_FlushAutomatically()
        {
            // Act
            _handler.MaxMetricsSameName(PowertoolsConfigurations.MaxMetrics);

            var metricsOutput = _consoleOut.OutputValues;

            // Assert

            // flush when it reaches MaxMetrics
            Assert.Contains(
                "\"Service\":\"testService\",\"Metric Name\":[0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99]}",
                metricsOutput[0]);

            // flush the (MaxMetrics + 1) item only
            Assert.Contains("\"Dimensions\":[[\"Service\"]]}]},\"Service\":\"testService\",\"Metric Name\":100}", metricsOutput[1]);
        }

        [Trait("Category", "EMFLimits")]
        [Fact]
        public void WhenMoreThan29DimensionsAdded_ThrowArgumentOutOfRangeException()
        {
            // Act
            var act = () => { _handler.MaxDimensions(29); };

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenNamespaceNotDefined_ThrowSchemaValidationException()
        {
            // Act
            var act = () => { _handler.NoNamespace(); };

            // Assert
            var exception = Assert.Throws<SchemaValidationException>(act);
            Assert.Equal("EMF schema is invalid. 'namespace' is mandatory and not specified.", exception.Message);
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenDimensionsAreAdded_MustExistAsMembers()
        {
            // Act
            _handler.AddDimensions();

            var metricsOutput = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\",\"functionVersion\"]]"
                , metricsOutput);
            Assert.Contains("\"Service\":\"testService\",\"functionVersion\":\"$LATEST\""
                , metricsOutput);
        }
        
        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void When_Multiple_DimensionsAreAdded_MustExistAsMembers()
        {
            // Act
            _handler.AddMultipleDimensions();

            var metricsOutput = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"ColdStart\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]]}]},\"Service\":\"ServiceName\",\"ColdStart\":1}", metricsOutput);
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"SingleMetric1\",\"Unit\":\"Count\",\"StorageResolution\":1}],\"Dimensions\":[[\"Default1\"]]}]},\"Default1\":\"SingleMetric1\",\"SingleMetric1\":1}", metricsOutput);
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"ns2\",\"Metrics\":[{\"Name\":\"SingleMetric2\",\"Unit\":\"Count\",\"StorageResolution\":1}],\"Dimensions\":[[\"Default1\",\"Default2\"]]}]},\"Default1\":\"SingleMetric2\",\"Default2\":\"SingleMetric2\",\"SingleMetric2\":1}", metricsOutput);
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"AddMetric\",\"Unit\":\"Count\",\"StorageResolution\":1},{\"Name\":\"AddMetric2\",\"Unit\":\"Count\",\"StorageResolution\":1}],\"Dimensions\":[[\"Service\"]]}]},\"Service\":\"ServiceName\",\"AddMetric\":1,\"AddMetric2\":1}", metricsOutput);
        }
        
        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void When_PushSingleMetric_With_Namespace()
        {
            // Act
            _handler.PushSingleMetricWithNamespace();

            var metricsOutput = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"ExampleApplication\",\"Metrics\":[{\"Name\":\"SingleMetric\",\"Unit\":\"Count\",\"StorageResolution\":1}],\"Dimensions\":[[\"Default\"]]}]},\"Default\":\"SingleMetric\",\"SingleMetric\":1}", metricsOutput);
        }
        
        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void When_PushSingleMetric_With_No_DefaultDimensions()
        {
            // Act
            _handler.PushSingleMetricNoDefaultDimensions();

            var metricsOutput = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"ExampleApplication\",\"Metrics\":[{\"Name\":\"SingleMetric\",\"Unit\":\"Count\"}],\"Dimensions\":[[]]}]},\"SingleMetric\":1}", metricsOutput);
        }
        
        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void When_PushSingleMetric_With_DefaultDimensions()
        {
            // Act
            _handler.PushSingleMetricDefaultDimensions();

            var metricsOutput = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"ExampleApplication\",\"Metrics\":[{\"Name\":\"SingleMetric\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Default\"]]}]},\"Default\":\"SingleMetric\",\"SingleMetric\":1}", metricsOutput);
        }
        
        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void When_PushSingleMetric_With_Env_Namespace()
        {
            // Arrange
            Environment.SetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE", "EnvNamespace");
            
            // Act
            _handler.PushSingleMetricWithEnvNamespace();

            var metricsOutput = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"EnvNamespace\",\"Metrics\":[{\"Name\":\"SingleMetric\",\"Unit\":\"Count\",\"StorageResolution\":1}],\"Dimensions\":[[\"Default\"]]}]},\"Default\":\"SingleMetric\",\"SingleMetric\":1}", metricsOutput);
            
            // assert with different service name
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"EnvNamespace\",\"Metrics\":[{\"Name\":\"SingleMetric2\",\"Unit\":\"Count\",\"StorageResolution\":1}],\"Dimensions\":[[\"Service\",\"Default\"]]}]},\"Service\":\"service1\",\"Default\":\"SingleMetric\",\"SingleMetric2\":1}", metricsOutput);
            
            // assert with different service name
            Assert.Contains("\"CloudWatchMetrics\":[{\"Namespace\":\"EnvNamespace\",\"Metrics\":[{\"Name\":\"SingleMetric3\",\"Unit\":\"Count\",\"StorageResolution\":1}],\"Dimensions\":[[\"Service\",\"Default\"]]}]},\"Service\":\"service2\",\"Default\":\"SingleMetric\",\"SingleMetric3\":1}", metricsOutput);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenNamespaceIsDefined_AbleToRetrieveNamespace()
        {
            // Act
            _handler.AddMetric();

            var metricsOutput = _consoleOut.ToString();

            var result = Metrics.Instance.Options.Namespace;

            // Assert
            Assert.Equal("dotnet-powertools-test", result);
            Assert.Contains("\"Namespace\":\"dotnet-powertools-test\"", metricsOutput);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsDefined_AbleToAddMetadata()
        {
            // Act
            _handler.AddMetadata();

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"test_metadata\":\"test_value\"", result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenDefaultDimensionsSet_ValidInitialization()
        {
            // Arrange
            var key = "CustomDefaultDimension";
            var value = "CustomDefaultDimensionValue";

            var defaultDimensions = new Dictionary<string, string> { { key, value } };

            // Act
            _handler.AddDefaultDimensions(defaultDimensions);

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains($"\"Dimensions\":[[\"Service\",\"{key}\"]]", result);
            Assert.Contains($"\"CustomDefaultDimension\":\"{value}\"", result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricIsNegativeValue_ThrowException()
        {
            // Act
            var act = () =>
            {
                const int metricValue = -1;
                _handler.AddMetric("TestMetric", metricValue);
            };

            // Assert
            var exception = Assert.Throws<ArgumentException>(act);
            Assert.Equal("'AddMetric' method requires a valid metrics value. Value must be >= 0. (Parameter 'value')",
                exception.Message);
        }

        [Trait("Category", "SchemaValidation")]
        [Fact]
        public void WhenDefaultDimensionSet_IgnoreDuplicates()
        {
            // Arrange

            var defaultDimensions = new Dictionary<string, string>
                { { "CustomDefaultDimension", "CustomDefaultDimensionValue" } };


            // Act
            _handler.AddDefaultDimensionsTwice(defaultDimensions);

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\",\"CustomDefaultDimension\"]]", result);
            Assert.Contains("\"CustomDefaultDimension\":\"CustomDefaultDimensionValue\"", result);
        }

        [Fact]
        public void WhenMetricsAndMetadataAdded_ValidateOutput()
        {
            // Act 
            _handler.AddDimensionMetricMetadata("Time", "env");

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains(
                "CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}],\"Dimensions\":[[\"Service\",\"functionVersion\"]]}]},\"Service\":\"testService\",\"functionVersion\":\"$LATEST\",\"Time\":100.7,\"env\":\"dev\"}"
                , result);
        }

        [Fact]
        public void When_Metrics_And_Metadata_Added_With_Same_Key_Should_Only_Write_Metrics()
        {
            // Act 
            _handler.AddDimensionMetricMetadata("Time", "Time");

            var result = _consoleOut.ToString();

            // Assert
            // No Metadata key was added
            Assert.Contains(
                "CloudWatchMetrics\":[{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}],\"Dimensions\":[[\"Service\",\"functionVersion\"]]}]},\"Service\":\"testService\",\"functionVersion\":\"$LATEST\",\"Time\":100.7}"
                , result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsWithSameNameAdded_ValidateMetricArray()
        {
            // Act 
            _handler.AddMetricSameName();

            var result = _consoleOut.ToString();

            // Debug output to see what's actually being written
            System.Diagnostics.Debug.WriteLine($"Actual output: {result}");

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\"}]"
                , result);
            Assert.Contains("\"Time\":[100.5,200]"
                , result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsWithStandardResolutionAdded_ValidateMetricArray()
        {
            // Act 
            _handler.AddMetric("Time", 100.5, MetricUnit.Milliseconds, MetricResolution.Standard);

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\",\"StorageResolution\":60}]"
                , result);
            Assert.Contains("\"Time\":100.5"
                , result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void WhenMetricsWithHighResolutionAdded_ValidateMetricArray()
        {
            // Act 
            _handler.AddMetric("Time", 100.5, MetricUnit.Milliseconds, MetricResolution.High);

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"Metrics\":[{\"Name\":\"Time\",\"Unit\":\"Milliseconds\",\"StorageResolution\":1}]"
                , result);
            Assert.Contains("\"Time\":100.5"
                , result);
        }
        
        [Fact]
        public async Task WhenMetricsAsyncRaceConditionItemSameKeyExists_ValidateLock()
        {
            // Act
            await _handler.RaceConditon();

            var metricsOutput = _consoleOut.ToString();

            // Assert
            Assert.Contains(
                "{\"Namespace\":\"dotnet-powertools-test\",\"Metrics\":[{\"Name\":\"Metric Name\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\"]]",
                metricsOutput);
        }
        
        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void AddDimensions_WithMultipleValues_AddsDimensionsToSameDimensionSet()
        {
            // Act
            _handler.AddMultipleDimensionsInSameSet();

            var result = _consoleOut.ToString();
    
            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\",\"Environment\",\"Region\"]]", result);
            Assert.Contains("\"Service\":\"testService\",\"Environment\":\"test\",\"Region\":\"us-west-2\"", result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void AddDimensions_WithEmptyArray_DoesNotAddAnyDimensions()
        {
            // Act
            _handler.AddEmptyDimensions();
    
            var result = _consoleOut.ToString();
    
            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\"]]", result);
            Assert.DoesNotContain("\"Environment\":", result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void AddDimensions_WithNullOrEmptyKey_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _handler.AddDimensionsWithInvalidKey());
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void AddDimensions_WithNullOrEmptyValue_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _handler.AddDimensionsWithInvalidValue());
        }
        
        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void AddDimensions_OverwritesExistingDimensions_LastValueWins()
        {
            // Act
            _handler.AddDimensionsWithOverwrite();

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"Service\":\"testService\",\"dimension1\":\"B\",\"dimension2\":\"2\"", result);
            Assert.DoesNotContain("\"dimension1\":\"A\"", result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void AddDimensions_IncludesDefaultDimensions()
        {
            // Act
            _handler.AddDimensionsWithDefaultDimensions();

            var result = _consoleOut.ToString();

            // Assert
            Assert.Contains("\"Dimensions\":[[\"Service\",\"environment\",\"dimension1\",\"dimension2\"]]", result);
            Assert.Contains("\"Service\":\"testService\",\"environment\":\"prod\",\"dimension1\":\"1\",\"dimension2\":\"2\"", result);
        }

        [Trait("Category", "MetricsImplementation")]
        [Fact]
        public void AddDefaultDimensionsAtRuntime_OnlyAppliedToNewDimensionSets()
        {
            // Act
            _handler.AddDefaultDimensionsAtRuntime();

            var result = _consoleOut.ToString();

            // First metric output should have original default dimensions
            Assert.Contains("\"Metrics\":[{\"Name\":\"FirstMetric\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\",\"environment\",\"dimension1\",\"dimension2\"]]", result);
            Assert.Contains("\"Service\":\"testService\",\"environment\":\"prod\",\"dimension1\":\"1\",\"dimension2\":\"2\",\"FirstMetric\":1", result);
    
            // Second metric output should have additional default dimensions
            Assert.Contains("\"Metrics\":[{\"Name\":\"SecondMetric\",\"Unit\":\"Count\"}],\"Dimensions\":[[\"Service\",\"environment\",\"tenantId\",\"foo\",\"bar\"]]", result);
            Assert.Contains("\"Service\":\"testService\",\"environment\":\"prod\",\"tenantId\":\"1\",\"foo\":\"1\",\"bar\":\"2\",\"SecondMetric\":1", result);
        }


        #region Helpers

        private List<int> AllIndexesOf(string str, string value)
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


        public void Dispose()
        {
            // need to reset instance after each test
            Metrics.ResetForTest();
            MetricsAspect.ResetForTest();
            Environment.SetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE", null);
        }
    }
}
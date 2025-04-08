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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Common.Core;
using AWS.Lambda.Powertools.Common.Tests;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Logging.Tests.Utilities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests
{
    [Collection("X Sequential")]
    public class PowertoolsLoggerTest : IDisposable
    {
        public PowertoolsLoggerTest()
        {
            // Logger.UseDefaultFormatter();
        }

        private static void Log_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel logLevel, LogLevel MinimumLogLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            var systemWrapper = Substitute.For<IConsoleWrapper>();

            // Configure the substitute for IPowertoolsConfigurations
            configurations.Service.Returns(service);
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());
            configurations.LogLevel.Returns(MinimumLogLevel.ToString());

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                MinimumLogLevel = MinimumLogLevel,
                LogOutput = systemWrapper // Set the output directly on configuration
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            switch (logLevel)
            {
                // Act
                case LogLevel.Critical:
                    logger.LogCritical("Test");
                    break;
                case LogLevel.Debug:
                    logger.LogDebug("Test");
                    break;
                case LogLevel.Error:
                    logger.LogError("Test");
                    break;
                case LogLevel.Information:
                    logger.LogInformation("Test");
                    break;
                case LogLevel.Trace:
                    logger.LogTrace("Test");
                    break;
                case LogLevel.Warning:
                    logger.LogWarning("Test");
                    break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s => s.Contains(service))
            );
        }

        private static void Log_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel logLevel,
            LogLevel MinimumLogLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            var systemWrapper = Substitute.For<IConsoleWrapper>();

            // Configure the substitute for IPowertoolsConfigurations
            configurations.Service.Returns(service);
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());
            configurations.LogLevel.Returns(MinimumLogLevel.ToString());

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = MinimumLogLevel,
                LogOutput = systemWrapper // Set the output directly on configuration
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            switch (logLevel)
            {
                // Act
                case LogLevel.Critical:
                    logger.LogCritical("Test");
                    break;
                case LogLevel.Debug:
                    logger.LogDebug("Test");
                    break;
                case LogLevel.Error:
                    logger.LogError("Test");
                    break;
                case LogLevel.Information:
                    logger.LogInformation("Test");
                    break;
                case LogLevel.Trace:
                    logger.LogTrace("Test");
                    break;
                case LogLevel.Warning:
                    logger.LogWarning("Test");
                    break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }

            // Assert
            systemWrapper.DidNotReceive().WriteLine(
                Arg.Any<string>()
            );
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        public void LogTrace_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel.Trace, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        public void LogDebug_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel.Debug, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        public void LogInformation_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel.Information, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        public void LogWarning_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel.Warning, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        public void LogError_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel.Error, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogCritical_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsBelowLogLevel_Logs(LogLevel.Critical, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogTrace_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel.Trace, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogDebug_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel.Debug, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogInformation_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel.Information, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogWarning_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel.Warning, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        public void LogError_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel.Error, MinimumLogLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogNone_WithAnyMinimumLogLevel_DoesNotLog(LogLevel MinimumLogLevel)
        {
            Log_WhenMinimumLogLevelIsAboveLogLevel_DoesNotLog(LogLevel.None, MinimumLogLevel);
        }

        [Fact]
        public void Log_ConfigurationIsNotProvided_ReadsFromEnvironmentVariables()
        {
            // Arrange
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 0.7;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerSampleRate.Returns(loggerSampleRate);

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = logLevel,
                LogOutput = systemWrapper,
                SamplingRate = loggerSampleRate
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger("test");

            logger.LogInformation("Test");

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s =>
                    s.Contains(service) &&
                    s.Contains(loggerSampleRate.ToString(CultureInfo.InvariantCulture))
                )
            );
        }

        [Fact]
        public void Log_SamplingRateGreaterThanRandom_ChangedLogLevelToDebug()
        {
            // Arrange
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 0.7;
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerSampleRate.Returns(loggerSampleRate);

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = logLevel,
                LogOutput = systemWrapper,
                SamplingRate = loggerSampleRate,
                Random = randomSampleRate
            };

            // Act
            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);

            var logger = provider.CreateLogger("test");

            logger.LogInformation("Test");

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s =>
                    s ==
                    $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {loggerSampleRate}, Sampler Value: {randomSampleRate}."
                )
            );
        }

        [Fact]
        public void Log_SamplingRateGreaterThanOne_SkipsSamplingRateConfiguration()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 2;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerSampleRate.Returns(loggerSampleRate);

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = logLevel,
                LogOutput = systemWrapper,
                SamplingRate = loggerSampleRate
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            logger.LogInformation("Test");

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s =>
                    s ==
                    $"Skipping sampling rate configuration because of invalid value. Sampling rate: {loggerSampleRate}"
                )
            );
        }

        [Fact]
        public void Log_EnvVarSetsCaseToCamelCase_OutputsCamelCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.CamelCase.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            // Act
            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s =>
                    s.Contains("\"message\":{\"propOne\":\"Value 1\",\"propTwo\":\"Value 2\"}")
                )
            );
        }

        [Fact]
        public void Log_AttributeSetsCaseToCamelCase_OutputsCamelCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LoggerOutputCase = LoggerOutputCase.CamelCase,
                LogOutput = systemWrapper
            };

            // Act
            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s =>
                    s.Contains("\"message\":{\"propOne\":\"Value 1\",\"propTwo\":\"Value 2\"}")
                )
            );
        }

        [Fact]
        public void Log_EnvVarSetsCaseToPascalCase_OutputsPascalCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s =>
                    s.Contains("\"Message\":{\"PropOne\":\"Value 1\",\"PropTwo\":\"Value 2\"}")
                )
            );
        }

        [Fact]
        public void Log_AttributeSetsCaseToPascalCase_OutputsPascalCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"Message\":{\"PropOne\":\"Value 1\",\"PropTwo\":\"Value 2\"}")
            ));
        }

        [Fact]
        public void Log_EnvVarSetsCaseToSnakeCase_OutputsSnakeCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.SnakeCase.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"message\":{\"prop_one\":\"Value 1\",\"prop_two\":\"Value 2\"}")
            ));
        }

        [Fact]
        public void Log_AttributeSetsCaseToSnakeCase_OutputsSnakeCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LoggerOutputCase = LoggerOutputCase.SnakeCase,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"message\":{\"prop_one\":\"Value 1\",\"prop_two\":\"Value 2\"}")
            ));
        }

        [Fact]
        public void Log_NoOutputCaseSet_OutputDefaultsToSnakeCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"message\":{\"prop_one\":\"Value 1\",\"prop_two\":\"Value 2\"}")));
        }

        [Fact]
        public void BeginScope_WhenScopeIsObject_ExtractScopeKeys()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = logLevel
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = (PowertoolsLogger)provider.CreateLogger(loggerName);

            var scopeKeys = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            using (var loggerScope = logger.BeginScope(scopeKeys) as PowertoolsLoggerScope)
            {
                // Assert
                Assert.NotNull(loggerScope);
                Assert.NotNull(loggerScope.ExtraKeys);
                Assert.Equal(2, loggerScope.ExtraKeys.Count);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropOne"));
                Assert.Equal(scopeKeys.PropOne, (string)loggerScope.ExtraKeys["PropOne"]);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropTwo"));
                Assert.Equal(scopeKeys.PropTwo, (string)loggerScope.ExtraKeys["PropTwo"]);
            }

            Assert.Null(logger.CurrentScope?.ExtraKeys);
        }

        [Fact]
        public void BeginScope_WhenScopeIsObjectDictionary_ExtractScopeKeys()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = logLevel
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = (PowertoolsLogger)provider.CreateLogger(loggerName);

            var scopeKeys = new Dictionary<string, object>
            {
                { "PropOne", "Value 1" },
                { "PropTwo", "Value 2" }
            };

            using (var loggerScope = logger.BeginScope(scopeKeys) as PowertoolsLoggerScope)
            {
                // Assert
                Assert.NotNull(loggerScope);
                Assert.NotNull(loggerScope.ExtraKeys);
                Assert.Equal(2, loggerScope.ExtraKeys.Count);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropOne"));
                Assert.Equal(scopeKeys["PropOne"], loggerScope.ExtraKeys["PropOne"]);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropTwo"));
                Assert.Equal(scopeKeys["PropTwo"], loggerScope.ExtraKeys["PropTwo"]);
            }

            Assert.Null(logger.CurrentScope?.ExtraKeys);
        }

        [Fact]
        public void BeginScope_WhenScopeIsStringDictionary_ExtractScopeKeys()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = logLevel
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = (PowertoolsLogger)provider.CreateLogger(loggerName);

            var scopeKeys = new Dictionary<string, string>
            {
                { "PropOne", "Value 1" },
                { "PropTwo", "Value 2" }
            };

            using (var loggerScope = logger.BeginScope(scopeKeys) as PowertoolsLoggerScope)
            {
                // Assert
                Assert.NotNull(loggerScope);
                Assert.NotNull(loggerScope.ExtraKeys);
                Assert.Equal(2, loggerScope.ExtraKeys.Count);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropOne"));
                Assert.Equal(scopeKeys["PropOne"], loggerScope.ExtraKeys["PropOne"]);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropTwo"));
                Assert.Equal(scopeKeys["PropTwo"], loggerScope.ExtraKeys["PropTwo"]);
            }

            Assert.Null(logger.CurrentScope?.ExtraKeys);
        }

        [Theory]
        [InlineData(LogLevel.Trace, true)]
        [InlineData(LogLevel.Debug, true)]
        [InlineData(LogLevel.Information, true)]
        [InlineData(LogLevel.Warning, true)]
        [InlineData(LogLevel.Error, true)]
        [InlineData(LogLevel.Critical, true)]
        [InlineData(LogLevel.Trace, false)]
        [InlineData(LogLevel.Debug, false)]
        [InlineData(LogLevel.Information, false)]
        [InlineData(LogLevel.Warning, false)]
        [InlineData(LogLevel.Error, false)]
        [InlineData(LogLevel.Critical, false)]
        public void Log_WhenExtraKeysIsObjectDictionary_AppendExtraKeys(LogLevel logLevel, bool logMethod)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());
            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = LogLevel.Trace,
                LogOutput = systemWrapper
            };
            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = (PowertoolsLogger)provider.CreateLogger(loggerName);

            var scopeKeys = new Dictionary<string, object>
            {
                { "PropOne", "Value 1" },
                { "PropTwo", "Value 2" }
            };

            if (logMethod)
            {
                logger.Log(logLevel, scopeKeys, message);
            }
            else
            {
                switch (logLevel)
                {
                    case LogLevel.Trace:
                        logger.LogTrace(scopeKeys, message);
                        break;
                    case LogLevel.Debug:
                        logger.LogDebug(scopeKeys, message);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(scopeKeys, message);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(scopeKeys, message);
                        break;
                    case LogLevel.Error:
                        logger.LogError(scopeKeys, message);
                        break;
                    case LogLevel.Critical:
                        logger.LogCritical(scopeKeys, message);
                        break;
                    case LogLevel.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                }
            }

            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains(scopeKeys.Keys.First()) &&
                s.Contains(scopeKeys.Keys.Last()) &&
                s.Contains(scopeKeys.Values.First().ToString()) &&
                s.Contains(scopeKeys.Values.Last().ToString())
            ));

            Assert.Null(logger.CurrentScope?.ExtraKeys);
        }

        [Theory]
        [InlineData(LogLevel.Trace, true)]
        [InlineData(LogLevel.Debug, true)]
        [InlineData(LogLevel.Information, true)]
        [InlineData(LogLevel.Warning, true)]
        [InlineData(LogLevel.Error, true)]
        [InlineData(LogLevel.Critical, true)]
        [InlineData(LogLevel.Trace, false)]
        [InlineData(LogLevel.Debug, false)]
        [InlineData(LogLevel.Information, false)]
        [InlineData(LogLevel.Warning, false)]
        [InlineData(LogLevel.Error, false)]
        [InlineData(LogLevel.Critical, false)]
        public void Log_WhenExtraKeysIsStringDictionary_AppendExtraKeys(LogLevel logLevel, bool logMethod)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());
            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = LogLevel.Trace,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = (PowertoolsLogger)provider.CreateLogger(loggerName);

            var scopeKeys = new Dictionary<string, string>
            {
                { "PropOne", "Value 1" },
                { "PropTwo", "Value 2" }
            };

            if (logMethod)
            {
                logger.Log(logLevel, scopeKeys, message);
            }
            else
            {
                switch (logLevel)
                {
                    case LogLevel.Trace:
                        logger.LogTrace(scopeKeys, message);
                        break;
                    case LogLevel.Debug:
                        logger.LogDebug(scopeKeys, message);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(scopeKeys, message);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(scopeKeys, message);
                        break;
                    case LogLevel.Error:
                        logger.LogError(scopeKeys, message);
                        break;
                    case LogLevel.Critical:
                        logger.LogCritical(scopeKeys, message);
                        break;
                    case LogLevel.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                }
            }

            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains(scopeKeys.Keys.First()) &&
                s.Contains(scopeKeys.Keys.Last()) &&
                s.Contains(scopeKeys.Values.First()) &&
                s.Contains(scopeKeys.Values.Last())
            ));

            Assert.Null(logger.CurrentScope?.ExtraKeys);
        }

        [Theory]
        [InlineData(LogLevel.Trace, true)]
        [InlineData(LogLevel.Debug, true)]
        [InlineData(LogLevel.Information, true)]
        [InlineData(LogLevel.Warning, true)]
        [InlineData(LogLevel.Error, true)]
        [InlineData(LogLevel.Critical, true)]
        [InlineData(LogLevel.Trace, false)]
        [InlineData(LogLevel.Debug, false)]
        [InlineData(LogLevel.Information, false)]
        [InlineData(LogLevel.Warning, false)]
        [InlineData(LogLevel.Error, false)]
        [InlineData(LogLevel.Critical, false)]
        public void Log_WhenExtraKeysAsObject_AppendExtraKeys(LogLevel logLevel, bool logMethod)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());
            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = service,
                MinimumLogLevel = LogLevel.Trace,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = (PowertoolsLogger)provider.CreateLogger(loggerName);

            var scopeKeys = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            if (logMethod)
            {
                logger.Log(logLevel, scopeKeys, message);
            }
            else
            {
                switch (logLevel)
                {
                    case LogLevel.Trace:
                        logger.LogTrace(scopeKeys, message);
                        break;
                    case LogLevel.Debug:
                        logger.LogDebug(scopeKeys, message);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(scopeKeys, message);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(scopeKeys, message);
                        break;
                    case LogLevel.Error:
                        logger.LogError(scopeKeys, message);
                        break;
                    case LogLevel.Critical:
                        logger.LogCritical(scopeKeys, message);
                        break;
                    case LogLevel.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
                }
            }

            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("PropOne") &&
                s.Contains("PropTwo") &&
                s.Contains(scopeKeys.PropOne) &&
                s.Contains(scopeKeys.PropTwo)
            ));

            Assert.Null(logger.CurrentScope?.ExtraKeys);
        }

        [Fact]
        public void Log_WhenException_LogsExceptionDetails()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var error = new InvalidOperationException("TestError");
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            try
            {
                throw error;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"exception\":{\"type\":\"" + error.GetType().FullName + "\",\"message\":\"" +
                           error.Message + "\"")
            ));
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains(
                    "\"exception\":{\"type\":\"System.InvalidOperationException\",\"message\":\"TestError\",\"source\":\"AWS.Lambda.Powertools.Logging.Tests\",\"stack_trace\":\"   at AWS.Lambda.Powertools.Logging.Tests.PowertoolsLoggerTest.Log_WhenException_LogsExceptionDetails()")
            ));
        }

        [Fact]
        public void Log_Inner_Exception()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var error = new InvalidOperationException("Parent exception message",
                new ArgumentNullException(nameof(service), "Very important inner exception message"));
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            logger.LogError(
                error,
                "Something went wrong and we logged an exception itself with an inner exception. This is a param {arg}",
                12345);

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"exception\":{\"type\":\"" + error.GetType().FullName + "\",\"message\":\"" +
                           error.Message + "\"")
            ));

            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"level\":\"Error\"") &&
                s.Contains("\"service\":\"" + service + "\"") &&
                s.Contains("\"name\":\"" + loggerName + "\"") &&
                s.Contains("\"message\":\"Something went wrong and we logged an exception itself with an inner exception. This is a param 12345\"") &&
                s.Contains("\"exception\":{") &&
                s.Contains("\"type\":\"System.InvalidOperationException\"") &&
                s.Contains("\"message\":\"Parent exception message\"") &&
                s.Contains("\"inner_exception\":{") &&
                s.Contains("\"type\":\"System.ArgumentNullException\"") &&
                s.Contains("\"message\":\"Very important inner exception message (Parameter 'service')\"")
            ));
        }

        [Fact]
        public void Log_Nested_Inner_Exception()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var error = new InvalidOperationException("Parent exception message",
                new ArgumentNullException(nameof(service),
                    new Exception("Very important nested inner exception message")));

            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);


            logger.LogError(
                error,
                "Something went wrong and we logged an exception itself with an inner exception. This is a param {arg}",
                12345);

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"message\":\"Something went wrong and we logged an exception itself with an inner exception. This is a param 12345\"") &&
                s.Contains("\"exception\":{") &&
                s.Contains("\"type\":\"System.InvalidOperationException\"") &&
                s.Contains("\"message\":\"Parent exception message\"") &&
                s.Contains("\"inner_exception\":{") &&
                s.Contains("\"type\":\"System.ArgumentNullException\"") &&
                s.Contains("\"message\":\"service\"") &&
                s.Contains("\"inner_exception\":{") &&
                s.Contains("\"type\":\"System.Exception\"") &&
                s.Contains("\"message\":\"Very important nested inner exception message\"")
            ));
        }

        [Fact]
        public void Log_WhenNestedException_LogsExceptionDetails()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var error = new InvalidOperationException("TestError");
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            try
            {
                throw error;
            }
            catch (Exception ex)
            {
                logger.LogInformation(new { Name = "Test Object", Error = ex });
            }

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"error\":{\"type\":\"" + error.GetType().FullName + "\",\"message\":\"" + error.Message +
                           "\"")
            ));
        }

        [Fact]
        public void Log_WhenByteArray_LogsBase64EncodedString()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var bytes = new byte[10];
            new Random().NextBytes(bytes);
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };
            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            // Act
            logger.LogInformation(new { Name = "Test Object", Bytes = bytes });

            // Assert
            var base64String = Convert.ToBase64String(bytes);
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains($"\"bytes\":\"{base64String}\"")
            ));
        }

        [Fact]
        public void Log_WhenMemoryStream_LogsBase64String()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var bytes = new byte[10];
            new Random().NextBytes(bytes);
            var memoryStream = new MemoryStream(bytes)
            {
                Position = 0
            };
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            // Act
            logger.LogInformation(new { Name = "Test Object", Stream = memoryStream });

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"stream\":\"" + Convert.ToBase64String(bytes) + "\"")
            ));
        }

        [Fact]
        public void Log_WhenMemoryStream_LogsBase64String_UnsafeRelaxedJsonEscaping()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();

            // This will produce the encoded string dW5zYWZlIHN0cmluZyB+IHRlc3Q= (which has a plus sign to test unsafe escaping)
            var bytes = Encoding.UTF8.GetBytes("unsafe string ~ test");

            var memoryStream = new MemoryStream(bytes)
            {
                Position = 0
            };
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            // Act
            logger.LogInformation(new { Name = "Test Object", Stream = memoryStream });

            // Assert
            systemWrapper.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains("\"stream\":\"" + Convert.ToBase64String(bytes) + "\"")
            ));
        }

        [Fact]
        public void Log_Set_Execution_Environment_Context()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();

            var env = new PowertoolsEnvironment();
            // Act
            var configurations = new PowertoolsConfigurations(env);

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);
            logger.LogInformation("Test");

            // Assert
            Assert.Contains($"{Constants.FeatureContextIdentifier}/Logging/",
                env.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
        }

        [Fact]
        public void Log_Skip_If_Exists_Execution_Environment_Context()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();

            var env = new PowertoolsEnvironment();
            env.SetEnvironmentVariable("AWS_EXECUTION_ENV",
                $"{Constants.FeatureContextIdentifier}/Logging/AlreadyThere");

            // Act
            var configurations = new PowertoolsConfigurations(env);

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);
            logger.LogInformation("Test");

            // Assert
            Assert.Equal($"{Constants.FeatureContextIdentifier}/Logging/AlreadyThere",
                env.GetEnvironmentVariable("AWS_EXECUTION_ENV"));
            env.SetEnvironmentVariable("AWS_EXECUTION_ENV", null);
        }

        [Fact]
        public void Log_Should_Serialize_DateOnly()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LoggerOutputCase = LoggerOutputCase.CamelCase,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2",
                PropThree = new
                {
                    PropFour = 1
                },
                Date = new DateOnly(2022, 1, 1)
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s =>
                    s.Contains(
                        "\"message\":{\"propOne\":\"Value 1\",\"propTwo\":\"Value 2\",\"propThree\":{\"propFour\":1},\"date\":\"2022-01-01\"}")
                )
            );
        }

        [Fact]
        public void Log_Should_Serialize_TimeOnly()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<IConsoleWrapper>();

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                Service = null,
                MinimumLogLevel = LogLevel.None,
                LoggerOutputCase = LoggerOutputCase.CamelCase,
                LogOutput = systemWrapper,
                Random = randomSampleRate
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2",
                Time = new TimeOnly(12, 0, 0)
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).WriteLine(
                Arg.Is<string>(s =>
                    s.Contains("\"message\":{\"propOne\":\"Value 1\",\"propTwo\":\"Value 2\",\"time\":\"12:00:00\"}")
                )
            );
        }


        [Theory]
        [InlineData("WARN", LogLevel.Warning)]
        [InlineData("Fatal", LogLevel.Critical)]
        [InlineData("NotValid", LogLevel.Critical)]
        [InlineData("NotValid", LogLevel.Warning)]
        public void Log_Should_Use_Powertools_Log_Level_When_Lambda_Log_Level_Enabled(string awsLogLevel,
            LogLevel logLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();

            var environment = Substitute.For<IPowertoolsEnvironment>();
            // Powertools Log Level takes precedence
            environment.GetEnvironmentVariable("POWERTOOLS_LOG_LEVEL").Returns(logLevel.ToString());
            environment.GetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL").Returns(awsLogLevel);

            var configurations = new PowertoolsConfigurations(environment);

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                LoggerOutputCase = LoggerOutputCase.CamelCase
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2",
                Time = new TimeOnly(12, 0, 0)
            };

            // Act
            logger.LogWarning(message);

            // Assert
            Assert.True(logger.IsEnabled(logLevel));
            Assert.Equal(logLevel, configurations.GetLogLevel());
        }

        [Theory]
        [InlineData("WARN", LogLevel.Warning)]
        [InlineData("Fatal", LogLevel.Critical)]
        public void Log_Should_Use_AWS_Lambda_Log_Level_When_Enabled(string awsLogLevel,
            LogLevel logLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();

            var environment = Substitute.For<IPowertoolsEnvironment>();
            // Powertools Log Level not set, AWS Lambda Log Level is used
            environment.GetEnvironmentVariable("POWERTOOLS_LOG_LEVEL").Returns(string.Empty);
            environment.GetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL").Returns(awsLogLevel);

            var configurations = new PowertoolsConfigurations(environment);

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                LoggerOutputCase = LoggerOutputCase.CamelCase,
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2",
                Time = new TimeOnly(12, 0, 0)
            };

            // Act
            logger.LogWarning(message);

            // Assert
            Assert.True(logger.IsEnabled(logLevel));
            Assert.Equal(LogLevel.Information, configurations.GetLogLevel()); //default
            Assert.Equal(logLevel, configurations.GetLambdaLogLevel());
        }

        [Fact]
        public void Log_Should_Show_Warning_When_AWS_Lambda_Log_Level_Enabled()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();

            var environment = Substitute.For<IPowertoolsEnvironment>();
            environment.GetEnvironmentVariable("POWERTOOLS_LOG_LEVEL").Returns("Debug");
            environment.GetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL").Returns("Warn");

            var systemWrapper = new TestLoggerOutput();
            var configurations = new PowertoolsConfigurations(environment);

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                LoggerOutputCase = LoggerOutputCase.CamelCase,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var logLevel = configurations.GetLogLevel();
            var lambdaLogLevel = configurations.GetLambdaLogLevel();

            // Assert
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.Equal(LogLevel.Debug, logLevel);
            Assert.Equal(LogLevel.Warning, lambdaLogLevel);

            Assert.Contains(
                $"Current log level ({logLevel}) does not match AWS Lambda Advanced Logging Controls minimum log level ({lambdaLogLevel}). This can lead to data loss, consider adjusting them.",
                systemWrapper.ToString());
        }

        [Theory]
        [InlineData(true, "LogLevel")]
        [InlineData(false, "Level")]
        public void Log_PascalCase_Outputs_Correct_Level_Property_When_AWS_Lambda_Log_Level_Enabled_Or_Disabled(
            bool alcEnabled, string levelProp)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();

            var environment = Substitute.For<IPowertoolsEnvironment>();
            environment.GetEnvironmentVariable("POWERTOOLS_LOG_LEVEL").Returns("Information");
            if (alcEnabled)
                environment.GetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL").Returns("Info");

            var systemWrapper = new TestLoggerOutput();
            var configurations = new PowertoolsConfigurations(environment);
            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                LoggerOutputCase = LoggerOutputCase.PascalCase,
                LogOutput = systemWrapper
            };
            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
            };

            logger.LogInformation(message);

            // Assert
            Assert.Contains($"\"{levelProp}\":\"Information\"", systemWrapper.ToString());
        }

        [Theory]
        [InlineData(LoggerOutputCase.CamelCase)]
        [InlineData(LoggerOutputCase.SnakeCase)]
        public void Log_CamelCase_Outputs_Level_When_AWS_Lambda_Log_Level_Enabled(LoggerOutputCase casing)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();

            var environment = Substitute.For<IPowertoolsEnvironment>();
            environment.GetEnvironmentVariable("POWERTOOLS_LOG_LEVEL").Returns(string.Empty);
            environment.GetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL").Returns("Info");

            var systemWrapper = new TestLoggerOutput();
            var configurations = new PowertoolsConfigurations(environment);
            configurations.LoggerOutputCase.Returns(casing.ToString());

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                LoggerOutputCase = casing,
                LogOutput = systemWrapper
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
            };

            logger.LogInformation(message);

            // Assert
            Assert.Contains("\"level\":\"Information\"", systemWrapper.ToString());
        }

        [Theory]
        [InlineData("TRACE", LogLevel.Trace)]
        [InlineData("debug", LogLevel.Debug)]
        [InlineData("Info", LogLevel.Information)]
        [InlineData("WARN", LogLevel.Warning)]
        [InlineData("ERROR", LogLevel.Error)]
        [InlineData("Fatal", LogLevel.Critical)]
        [InlineData("DoesNotExist", LogLevel.None)]
        public void Should_Map_AWS_Log_Level_And_Default_To_Information(string awsLogLevel, LogLevel logLevel)
        {
            // Arrange
            var environment = Substitute.For<IPowertoolsEnvironment>();
            environment.GetEnvironmentVariable("AWS_LAMBDA_LOG_LEVEL").Returns(awsLogLevel);

            var configuration = new PowertoolsConfigurations(environment);

            // Act
            var logLvl = configuration.GetLambdaLogLevel();

            // Assert
            Assert.Equal(logLevel, logLvl);
        }

        [Theory]
        [InlineData(true, LogLevel.Warning)]
        [InlineData(false, LogLevel.Critical)]
        public void Log_Should_Use_Powertools_Log_Level_When_Set(bool willLog, LogLevel logLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var environment = Substitute.For<IPowertoolsEnvironment>();
            environment.GetEnvironmentVariable("POWERTOOLS_LOG_LEVEL").Returns(logLevel.ToString());

            var configurations = new PowertoolsConfigurations(environment);

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                LoggerOutputCase = LoggerOutputCase.CamelCase
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger(loggerName);

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2",
                Time = new TimeOnly(12, 0, 0)
            };

            // Act
            logger.LogWarning(message);

            // Assert
            Assert.True(logger.IsEnabled(logLevel));
            Assert.Equal(logLevel.ToString(), configurations.LogLevel);
        }
        
        [Theory]
        [InlineData(true, "on-demand")]
        [InlineData(false, "provisioned-concurrency")]
        public void Log_Cold_Start(bool willLog, string awsInitType)
        {
            // Arrange
            var logOutput = new TestLoggerOutput();
            Environment.SetEnvironmentVariable("AWS_LAMBDA_INITIALIZATION_TYPE", awsInitType);
            var configurations = new PowertoolsConfigurations(new PowertoolsEnvironment());

            var loggerConfiguration = new PowertoolsLoggerConfiguration
            {
                LoggerOutputCase = LoggerOutputCase.CamelCase,
                LogOutput = logOutput
            };

            var provider = new PowertoolsLoggerProvider(loggerConfiguration, configurations);
            var logger = provider.CreateLogger("temp");

            // Act
            logger.LogInformation("Hello");

            var outPut = logOutput.ToString();
            // Assert
            Assert.Contains($"\"coldStart\":{willLog.ToString().ToLower()}", outPut);
        }

        public void Dispose()
        {
            // Environment.SetEnvironmentVariable("AWS_LAMBDA_INITIALIZATION_TYPE", null);
            LambdaLifecycleTracker.Reset();
        }
    }
}
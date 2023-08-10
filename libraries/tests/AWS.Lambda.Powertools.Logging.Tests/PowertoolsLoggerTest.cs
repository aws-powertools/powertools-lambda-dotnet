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
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests
{
    public class PowertoolsLoggerTest
    {
        private static void Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel logLevel, LogLevel minimumLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            var systemWrapper = Substitute.For<ISystemWrapper>();

            // Configure the substitute for IPowertoolsConfigurations
            configurations.Service.Returns(service);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = minimumLevel
                });

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
            systemWrapper.Received(1).LogLine(
                Arg.Is<string>(s => s.Contains(service))
            );
        }

        private static void Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel logLevel, LogLevel minimumLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            var systemWrapper = Substitute.For<ISystemWrapper>();

            // Configure the substitute for IPowertoolsConfigurations
            configurations.Service.Returns(service);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = minimumLevel
                });

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
            systemWrapper.DidNotReceive().LogLine(
                Arg.Any<string>()
            );
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        public void LogTrace_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Trace, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        public void LogDebug_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Debug, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        public void LogInformation_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Information, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        public void LogWarning_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Warning, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        public void LogError_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Error, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogCritical_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsBelowLogLevel_Logs(LogLevel.Critical, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogTrace_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Trace, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogDebug_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Debug, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogInformation_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Information, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogWarning_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Warning, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        public void LogError_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.Error, minimumLevel);
        }

        [Theory]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Warning)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Critical)]
        public void LogNone_WithAnyMinimumLevel_DoesNotLog(LogLevel minimumLevel)
        {
            Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel.None, minimumLevel);
        }

        [Fact]
        public void Log_ConfigurationIsNotProvided_ReadsFromEnvironmentVariables()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 0.7;
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerSampleRate.Returns(loggerSampleRate);

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            logger.LogInformation("Test");

            // Assert
            systemWrapper.Received(1).LogLine(
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
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 0.7;
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerSampleRate.Returns(loggerSampleRate);

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            logger.LogInformation("Test");

            // Assert
            systemWrapper.Received(1).LogLine(
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

            var systemWrapper = Substitute.For<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            logger.LogInformation("Test");

            // Assert
            systemWrapper.Received(1).LogLine(
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.CamelCase.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).LogLine(
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null,
                    LoggerOutputCase = LoggerOutputCase.CamelCase
                });

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).LogLine(
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).LogLine(
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null,
                    LoggerOutputCase = LoggerOutputCase.PascalCase
                });

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.SnakeCase.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null,
                    LoggerOutputCase = LoggerOutputCase.SnakeCase
                });

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            var message = new
            {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
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
            var systemWrapper = Substitute.For<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = logLevel
                });

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
            var systemWrapper = Substitute.For<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = logLevel
                });

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
            var systemWrapper = Substitute.For<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = logLevel
                });

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
            var systemWrapper = Substitute.For<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = LogLevel.Trace,
                });

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

            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
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
            var systemWrapper = Substitute.For<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = LogLevel.Trace,
                });

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

            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
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
            var systemWrapper = Substitute.For<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = LogLevel.Trace,
                });

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

            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            try
            {
                throw error;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
                s.Contains("\"exception\":{\"type\":\"" + error.GetType().FullName + "\",\"message\":\"" +
                           error.Message + "\"")
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            try
            {
                throw error;
            }
            catch (Exception ex)
            {
                logger.LogInformation(new { Name = "Test Object", Error = ex });
            }

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
                s.Contains("\"error\":{\"type\":\"" + error.GetType().FullName + "\",\"message\":\"" + error.Message +
                           "\"")
            ));
        }

        [Fact]
        public void Log_WhenByteArray_LogsByteArrayNumbers()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var bytes = new byte[10];
            new Random().NextBytes(bytes);
            var logLevel = LogLevel.Information;
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            // Act
            logger.LogInformation(new { Name = "Test Object", Bytes = bytes });

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
                s.Contains("\"bytes\":[" + string.Join(",", bytes) + "]")
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            // Act
            logger.LogInformation(new { Name = "Test Object", Stream = memoryStream });

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
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
            var randomSampleRate = 0.5;

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LogLevel.Returns(logLevel.ToString());

            var systemWrapper = Substitute.For<ISystemWrapper>();
            systemWrapper.GetRandom().Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });

            // Act
            logger.LogInformation(new { Name = "Test Object", Stream = memoryStream });

            // Assert
            systemWrapper.Received(1).LogLine(Arg.Is<string>(s =>
                s.Contains("\"stream\":\"" + Convert.ToBase64String(bytes) + "\"")
            ));
        }

        [Fact]
        public void Log_Set_Execution_Environment_Context()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var assemblyName = "AWS.Lambda.Powertools.Logger";
            var assemblyVersion = "1.0.0";

            var env = Substitute.For<IPowertoolsEnvironment>();
            env.GetAssemblyName(Arg.Any<PowertoolsLogger>()).Returns(assemblyName);
            env.GetAssemblyVersion(Arg.Any<PowertoolsLogger>()).Returns(assemblyVersion);

            // Act
            var wrapper = new SystemWrapper(env);
            var conf = new PowertoolsConfigurations(wrapper);

            var logger = new PowertoolsLogger(loggerName, conf, wrapper, () =>
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });
            logger.LogInformation("Test");

            // Assert
            env.Received(1).SetEnvironmentVariable("AWS_EXECUTION_ENV",
                $"{Constants.FeatureContextIdentifier}/Logger/{assemblyVersion}");
            env.Received(1).GetEnvironmentVariable("AWS_EXECUTION_ENV");
        }
    }
}
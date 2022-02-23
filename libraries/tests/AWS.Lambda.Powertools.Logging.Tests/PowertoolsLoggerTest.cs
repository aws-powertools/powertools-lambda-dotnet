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
using System.Linq;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using Moq;
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

            var configurations = new Mock<IPowertoolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
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
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>(s=> s.Contains(service))
                ), Times.Once);
           
        }
        
        private static void Log_WhenMinimumLevelIsAboveLogLevel_DoesNotLog(LogLevel logLevel, LogLevel minimumLevel)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();

            var configurations = new Mock<IPowertoolsConfigurations>();
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
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
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.IsAny<string>()
                ), Times.Never);
           
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
           
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerSampleRate).Returns(loggerSampleRate);
            
            var systemWrapper = new Mock<ISystemWrapper>();
            systemWrapper.Setup(c => c.GetRandom()).Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });
            
            logger.LogInformation("Test");

            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s.Contains(service) &&
                        s.Contains(loggerSampleRate.ToString(CultureInfo.InvariantCulture))
                    )
                ), Times.Once);
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
           
            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerSampleRate).Returns(loggerSampleRate);
            
            var systemWrapper = new Mock<ISystemWrapper>();
            systemWrapper.Setup(c => c.GetRandom()).Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });
            
            logger.LogInformation("Test");

            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s == $"Changed log level to DEBUG based on Sampling configuration. Sampling Rate: {loggerSampleRate}, Sampler Value: {randomSampleRate}."
                    )
                ), Times.Once);
           
        }
        
        [Fact]
        public void Log_SamplingRateGreaterThanOne_SkipsSamplingRateConfiguration()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Trace;
            var loggerSampleRate = 2;

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerSampleRate).Returns(loggerSampleRate);
            
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
                new LoggerConfiguration
                {
                    Service = null,
                    MinimumLevel = null
                });
            
            logger.LogInformation("Test");

            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s == $"Skipping sampling rate configuration because of invalid value. Sampling rate: {loggerSampleRate}"
                    )
                ), Times.Once);
           
        }

        [Fact]
        public void Log_SetsCaseToCamelCase_OutputsCamelCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            var randomSampleRate = 0.5;

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerOutputCase).Returns("CamelCase");

            var systemWrapper = new Mock<ISystemWrapper>();
            systemWrapper.Setup(c => c.GetRandom()).Returns(randomSampleRate);            

            var logger = new PowertoolsLogger(loggerName, configurations.Object, systemWrapper.Object, () =>
                 new LoggerConfiguration
                 {                     
                     Service = null,
                     MinimumLevel = null
                 });
            
            var message = new {
                PropOne = "Value 1",
                PropTwo = "Value 2"
            };

            logger.LogInformation(message);

            // Assert
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s =>
                        s.Contains("\"message\":{\"propOne\":\"Value 1\",\"propTwo\":\"Value 2\"}")
                    )
                ), Times.Once);
        }

        [Fact]
        public void Log_SetsCaseToPascalCase_OutputsPascalCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            var randomSampleRate = 0.5;

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerOutputCase).Returns("PascalCase");

            var systemWrapper = new Mock<ISystemWrapper>();
            systemWrapper.Setup(c => c.GetRandom()).Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations.Object, systemWrapper.Object, () =>
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
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s =>
                        s.Contains("\"Message\":{\"PropOne\":\"Value 1\",\"PropTwo\":\"Value 2\"}")
                    )
                ), Times.Once);
        }

        [Fact]
        public void Log_SetsCaseToSnakeCase_OutputsSnakeCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            var randomSampleRate = 0.5;

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerOutputCase).Returns("SnakeCase");

            var systemWrapper = new Mock<ISystemWrapper>();
            systemWrapper.Setup(c => c.GetRandom()).Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations.Object, systemWrapper.Object, () =>
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
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s =>
                        s.Contains("\"message\":{\"prop_one\":\"Value 1\",\"prop_two\":\"Value 2\"}")
                    )
                ), Times.Once);
        }

        [Fact]
        public void Log_NoOutputCaseSet_OutputDefaultsToSnakeCaseLog()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            var randomSampleRate = 0.5;

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);

            var systemWrapper = new Mock<ISystemWrapper>();
            systemWrapper.Setup(c => c.GetRandom()).Returns(randomSampleRate);

            var logger = new PowertoolsLogger(loggerName, configurations.Object, systemWrapper.Object, () =>
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
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s =>
                        s.Contains("\"message\":{\"prop_one\":\"Value 1\",\"prop_two\":\"Value 2\"}")
                    )
                ), Times.Once);
        }
        
        [Fact]
        public void BeginScope_WhenScopeIsObject_ExtractScopeKeys()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
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
                Assert.NotNull(loggerScope);
                Assert.NotNull(loggerScope.ExtraKeys);
                Assert.True(loggerScope.ExtraKeys.Count == 2);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropOne"));
                Assert.True((string)loggerScope.ExtraKeys["PropOne"] == scopeKeys.PropOne);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropTwo"));
                Assert.True((string)loggerScope.ExtraKeys["PropTwo"] == scopeKeys.PropTwo);
            }
            Assert.Null(logger.CurrentScope?.ExtraKeys);
        }
        
        [Fact]
        public void BeginScope_WhenScopeIsDictionary_ExtractScopeKeys()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
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
                Assert.NotNull(loggerScope);
                Assert.NotNull(loggerScope.ExtraKeys);
                Assert.True(loggerScope.ExtraKeys.Count == 2);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropOne"));
                Assert.True((string)loggerScope.ExtraKeys["PropOne"] == scopeKeys["PropOne"]);
                Assert.True(loggerScope.ExtraKeys.ContainsKey("PropTwo"));
                Assert.True((string)loggerScope.ExtraKeys["PropTwo"] == scopeKeys["PropTwo"]);
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
        public void Log_WhenExtraKeysIsStringDictionary_AppendExtraKeys(LogLevel logLevel, bool logMethod)
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerOutputCase).Returns("PascalCase");
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
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

            if(logMethod)
                logger.Log(logLevel, scopeKeys, message);
            else switch (logLevel)
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
            
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s.Contains(scopeKeys.Keys.First()) && 
                        s.Contains(scopeKeys.Keys.Last()) &&
                        s.Contains(scopeKeys.Values.First().ToString()) && 
                        s.Contains(scopeKeys.Values.Last().ToString())
                    )
                ), Times.Once);

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

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerOutputCase).Returns("PascalCase");
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
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

            if(logMethod)
                logger.Log(logLevel, scopeKeys, message);
            else switch (logLevel)
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
            
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s.Contains(scopeKeys.Keys.First()) && 
                        s.Contains(scopeKeys.Keys.Last()) &&
                        s.Contains(scopeKeys.Values.First()) && 
                        s.Contains(scopeKeys.Values.Last())
                    )
                ), Times.Once);

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

            var configurations = new Mock<IPowertoolsConfigurations>();
            configurations.Setup(c => c.Service).Returns(service);
            configurations.Setup(c => c.LogLevel).Returns(logLevel.ToString);
            configurations.Setup(c => c.LoggerOutputCase).Returns("PascalCase");
            var systemWrapper = new Mock<ISystemWrapper>();

            var logger = new PowertoolsLogger(loggerName,configurations.Object, systemWrapper.Object, () => 
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

            if(logMethod)
                logger.Log(logLevel, scopeKeys, message);
            else switch (logLevel)
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
            
            systemWrapper.Verify(v =>
                v.LogLine(
                    It.Is<string>
                    (s=> 
                        s.Contains("PropOne") && 
                        s.Contains("PropTwo") &&
                        s.Contains(scopeKeys.PropOne) && 
                        s.Contains(scopeKeys.PropTwo)
                    )
                ), Times.Once);

            Assert.Null(logger.CurrentScope?.ExtraKeys);
        }
    }
}
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
using System.Linq;
using System.Text.Json;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests
{
    [Collection("Sequential")]
    public class LogFormatterTest
    {
        [Fact]
        public void Log_WhenCustomFormatter_LogsCustomFormat()
        {
            // Arrange
            const bool coldStart = false;
            var xrayTraceId = Guid.NewGuid().ToString();
            var correlationId = Guid.NewGuid().ToString();
            var logLevel = LogLevel.Information;
            var minimumLevel = LogLevel.Information;
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();

            Logger.AppendKey(LoggingConstants.KeyColdStart, coldStart);
            Logger.AppendKey(LoggingConstants.KeyXRayTraceId, xrayTraceId);
            Logger.AppendKey(LoggingConstants.KeyCorrelationId, correlationId);

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);

            var globalExtraKeys = new Dictionary<string, object>
            {
                { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
                { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
            };
            Logger.AppendKeys(globalExtraKeys);

            var lambdaContext = new LogEntryLambdaContext
            {
                FunctionName = Guid.NewGuid().ToString(),
                FunctionVersion = Guid.NewGuid().ToString(),
                InvokedFunctionArn = Guid.NewGuid().ToString(),
                AwsRequestId = Guid.NewGuid().ToString(),
                MemoryLimitInMB = (new Random()).Next()
            };

            var eventArgs = new AspectEventArgs
            {
                Name = Guid.NewGuid().ToString(),
                Args = new object[]
                {
                    new
                    {
                        Source = "Test"
                    },
                    lambdaContext
                }
            };
            PowertoolsLambdaContext.Extract(eventArgs);

            var logFormatter = Substitute.For<ILogFormatter>();
            var formattedLogEntry = new
            {
                Message = message,
                Service = service,
                CorrelationIds = new
                {
                    lambdaContext.AwsRequestId,
                    XRayTraceId = xrayTraceId
                },
                LambdaFunction = new
                {
                    Name = lambdaContext.FunctionName,
                    Arn = lambdaContext.InvokedFunctionArn,
                    MemorySize = lambdaContext.MemoryLimitInMB,
                    Version = lambdaContext.FunctionVersion,
                    ColdStart = coldStart,
                },
                Level = logLevel.ToString(),
                Logger = new
                {
                    Name = loggerName
                }
            };

            logFormatter.FormatLogEntry(new LogEntry()).ReturnsForAnyArgs(formattedLogEntry);
            Logger.UseFormatter(logFormatter);

            var systemWrapper = Substitute.For<ISystemWrapper>();
            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = minimumLevel,
                    LoggerOutputCase = LoggerOutputCase.PascalCase
                });

            var scopeExtraKeys = new Dictionary<string, object>
            {
                { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
                { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
            };

            // Act
            logger.LogInformation(scopeExtraKeys, message);

            // Assert
            logFormatter.Received(1).FormatLogEntry(Arg.Is<LogEntry>
            (
                x =>
                    x.ColdStart == coldStart &&
                    x.XRayTraceId == xrayTraceId &&
                    x.CorrelationId == correlationId &&
                    x.Service == service &&
                    x.Name == loggerName &&
                    x.Level == logLevel &&
                    x.Message.ToString() == message &&
                    x.Exception == null &&
                    x.ExtraKeys != null &&
                    x.ExtraKeys.Count == globalExtraKeys.Count + scopeExtraKeys.Count &&
                    x.ExtraKeys.ContainsKey(globalExtraKeys.First().Key) &&
                    x.ExtraKeys[globalExtraKeys.First().Key] == globalExtraKeys.First().Value &&
                    x.ExtraKeys.ContainsKey(globalExtraKeys.Last().Key) &&
                    x.ExtraKeys[globalExtraKeys.Last().Key] == globalExtraKeys.Last().Value &&
                    x.ExtraKeys.ContainsKey(scopeExtraKeys.First().Key) &&
                    x.ExtraKeys[scopeExtraKeys.First().Key] == scopeExtraKeys.First().Value &&
                    x.ExtraKeys.ContainsKey(scopeExtraKeys.Last().Key) &&
                    x.ExtraKeys[scopeExtraKeys.Last().Key] == scopeExtraKeys.Last().Value &&
                    x.LambdaContext != null &&
                    x.LambdaContext.FunctionName == lambdaContext.FunctionName &&
                    x.LambdaContext.FunctionVersion == lambdaContext.FunctionVersion &&
                    x.LambdaContext.MemoryLimitInMB == lambdaContext.MemoryLimitInMB &&
                    x.LambdaContext.InvokedFunctionArn == lambdaContext.InvokedFunctionArn &&
                    x.LambdaContext.AwsRequestId == lambdaContext.AwsRequestId
            ));
            systemWrapper.Received(1).LogLine(JsonSerializer.Serialize(formattedLogEntry));
            
            //Clean up
            Logger.UseDefaultFormatter();
            Logger.RemoveAllKeys();
            PowertoolsLambdaContext.Clear();
            LoggingAspectHandler.ResetForTest();
        }
    }
    
    [Collection("Sequential")]
    public class LogFormatterNullTest
    {
        [Fact]
        public void Log_WhenCustomFormatterReturnNull_LogsError()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);

            var logFormatter = Substitute.For<ILogFormatter>();
            logFormatter.FormatLogEntry(new LogEntry()).ReturnsNullForAnyArgs();
            Logger.UseFormatter(logFormatter);

            var systemWrapper = Substitute.For<ISystemWrapper>();
            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = LogLevel.Information,
                    LoggerOutputCase = LoggerOutputCase.PascalCase
                });

            // Act
            logger.LogInformation(message);

            // Assert
            logFormatter.Received(1).FormatLogEntry(Arg.Any<LogEntry>());
            systemWrapper.Received(1).LogLine(Arg.Is<string>(x => x.Contains("Error") && x.Contains("null")));
            
            //Clean up
            Logger.UseDefaultFormatter();
        }
    }
    
    [Collection("Sequential")]
    public class LogFormatterExceptionTest
    {
        [Fact]
        public void Log_WhenCustomFormatterRaisesException_LogsError()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();
            var errorMessage = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);

            var logFormatter = Substitute.For<ILogFormatter>();
            logFormatter.FormatLogEntry(new LogEntry()).ThrowsForAnyArgs(new Exception(errorMessage));
            Logger.UseFormatter(logFormatter);

            var systemWrapper = Substitute.For<ISystemWrapper>();
            var logger = new PowertoolsLogger(loggerName, configurations, systemWrapper, () =>
                new LoggerConfiguration
                {
                    Service = service,
                    MinimumLevel = LogLevel.Information,
                    LoggerOutputCase = LoggerOutputCase.PascalCase
                });

            // Act
            logger.LogInformation(message);

            // Assert
            logFormatter.Received(1).FormatLogEntry(Arg.Any<LogEntry>());
            systemWrapper.Received(1).LogLine(Arg.Is<string>(x => x.Contains("Error") && x.Contains(errorMessage)));
            
            //Clean up
            Logger.UseDefaultFormatter();
        }
    }
}
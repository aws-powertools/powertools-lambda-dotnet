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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Serializers;
using AWS.Lambda.Powertools.Logging.Tests.Handlers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AWS.Lambda.Powertools.Logging.Tests.Formatter
{
    [Collection("Sequential")]
    public class LogFormatterTest : IDisposable
    {
        private readonly TestHandlers _testHandler;

        public LogFormatterTest()
        {
            _testHandler = new TestHandlers();
        }
        
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

            var loggerConfiguration = new LoggerConfiguration
            {
                Service = service,
                MinimumLevel = minimumLevel,
                LoggerOutputCase = LoggerOutputCase.PascalCase
            };

            var globalExtraKeys = new Dictionary<string, object>
            {
                { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
                { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() }
            };
            Logger.AppendKeys(globalExtraKeys);

            var lambdaContext = new TestLambdaContext
            {
                FunctionName = Guid.NewGuid().ToString(),
                FunctionVersion = Guid.NewGuid().ToString(),
                InvokedFunctionArn = Guid.NewGuid().ToString(),
                AwsRequestId = Guid.NewGuid().ToString(),
                MemoryLimitInMB = (new Random()).Next()
            };

            var args = Substitute.For<AspectEventArgs>();
            var method = Substitute.For<MethodInfo>();
            var parameter = Substitute.For<ParameterInfo>();

            // Setup parameter
            parameter.ParameterType.Returns(typeof(ILambdaContext));

            // Setup method
            method.GetParameters().Returns(new[] { parameter });

            // Setup args
            args.Method = method;
            args.Args = new object[] { lambdaContext };

            // Act

            LoggingLambdaContext.Extract(args);

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

            var provider = new LoggerProvider(loggerConfiguration, configurations, systemWrapper);
            var logger = provider.CreateLogger(loggerName);

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
                    x.ExtraKeys != null && (
                        x.ExtraKeys.Count != globalExtraKeys.Count + scopeExtraKeys.Count || (
                            x.ExtraKeys.Count == globalExtraKeys.Count + scopeExtraKeys.Count &&
                            x.ExtraKeys.ContainsKey(globalExtraKeys.First().Key) &&
                            x.ExtraKeys[globalExtraKeys.First().Key] == globalExtraKeys.First().Value &&
                            x.ExtraKeys.ContainsKey(globalExtraKeys.Last().Key) &&
                            x.ExtraKeys[globalExtraKeys.Last().Key] == globalExtraKeys.Last().Value &&
                            x.ExtraKeys.ContainsKey(scopeExtraKeys.First().Key) &&
                            x.ExtraKeys[scopeExtraKeys.First().Key] == scopeExtraKeys.First().Value &&
                            x.ExtraKeys.ContainsKey(scopeExtraKeys.Last().Key) &&
                            x.ExtraKeys[scopeExtraKeys.Last().Key] == scopeExtraKeys.Last().Value)) &&
                    x.LambdaContext != null &&
                    x.LambdaContext.FunctionName == lambdaContext.FunctionName &&
                    x.LambdaContext.FunctionVersion == lambdaContext.FunctionVersion &&
                    x.LambdaContext.MemoryLimitInMB == lambdaContext.MemoryLimitInMB &&
                    x.LambdaContext.InvokedFunctionArn == lambdaContext.InvokedFunctionArn &&
                    x.LambdaContext.AwsRequestId == lambdaContext.AwsRequestId
            ));

            systemWrapper.Received(1).LogLine(JsonSerializer.Serialize(formattedLogEntry));
        }

        [Fact]
        public void Should_Log_CustomFormatter_When_Decorated()
        {
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);
            var lambdaContext = new TestLambdaContext
            {
                FunctionName = "funtionName",
                FunctionVersion = "version",
                InvokedFunctionArn = "function::arn",
                AwsRequestId = "requestId",
                MemoryLimitInMB = 128
            };

            Logger.UseFormatter(new CustomLogFormatter());
            _testHandler.TestCustomFormatterWithDecorator("test", lambdaContext);

            // serializer works differently in .net 8 and AOT. In .net 6 it writes properties that have null
            // in .net 8 it removes null properties

#if NET8_0_OR_GREATER
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i =>
                    i.Contains(
                    "\"correlation_ids\":{\"aws_request_id\":\"requestId\"},\"lambda_function\":{\"name\":\"funtionName\",\"arn\":\"function::arn\",\"memory_limit_in_mb\":128,\"version\":\"version\",\"cold_start\":true},\"level\":\"Information\""))
            );
#else
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i =>
                    i.Contains(
                    "{\"message\":\"test\",\"service\":\"my_service\",\"correlation_ids\":{\"aws_request_id\":\"requestId\",\"x_ray_trace_id\":null,\"correlation_id\":null},\"lambda_function\":{\"name\":\"funtionName\",\"arn\":\"function::arn\",\"memory_limit_in_m_b\":128,\"version\":\"version\",\"cold_start\":true},\"level\":\"Information\",\"timestamp\":\"2024-01-01T00:00:00.0000000\",\"logger\":{\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"sample_rate\""))
            );
#endif
        }

        [Fact]
        public void Should_Log_CustomFormatter_When_No_Decorated_Just_Log()
        {
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);
            var lambdaContext = new TestLambdaContext
            {
                FunctionName = "funtionName",
                FunctionVersion = "version",
                InvokedFunctionArn = "function::arn",
                AwsRequestId = "requestId",
                MemoryLimitInMB = 128
            };

            Logger.UseFormatter(new CustomLogFormatter());

            _testHandler.TestCustomFormatterNoDecorator("test", lambdaContext);

            // serializer works differently in .net 8 and AOT. In .net 6 it writes properties that have null
            // in .net 8 it removes null properties

#if NET8_0_OR_GREATER
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i =>
                    i ==
                    "{\"message\":\"test\",\"service\":\"service_undefined\",\"correlation_ids\":{},\"lambda_function\":{\"cold_start\":true},\"level\":\"Information\",\"timestamp\":\"2024-01-01T00:00:00.0000000\",\"logger\":{\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"sample_rate\":0}}")
            );
#else
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i =>
                    i ==
                    "{\"message\":\"test\",\"service\":\"service_undefined\",\"correlation_ids\":{\"aws_request_id\":null,\"x_ray_trace_id\":null,\"correlation_id\":null},\"lambda_function\":{\"name\":null,\"arn\":null,\"memory_limit_in_m_b\":null,\"version\":null,\"cold_start\":true},\"level\":\"Information\",\"timestamp\":\"2024-01-01T00:00:00.0000000\",\"logger\":{\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"sample_rate\":0}}")
            );
#endif
        }

        [Fact]
        public void Should_Log_CustomFormatter_When_Decorated_No_Context()
        {
            var consoleOut = Substitute.For<StringWriter>();
            SystemWrapper.Instance.SetOut(consoleOut);

            Logger.UseFormatter(new CustomLogFormatter());

            _testHandler.TestCustomFormatterWithDecoratorNoContext("test");

#if NET8_0_OR_GREATER
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i =>
                    i ==
                    "{\"message\":\"test\",\"service\":\"my_service\",\"correlation_ids\":{},\"lambda_function\":{\"cold_start\":true},\"level\":\"Information\",\"timestamp\":\"2024-01-01T00:00:00.0000000\",\"logger\":{\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"sample_rate\":0.2}}")
            );
#else
            consoleOut.Received(1).WriteLine(
                Arg.Is<string>(i =>
                    i ==
                    "{\"message\":\"test\",\"service\":\"my_service\",\"correlation_ids\":{\"aws_request_id\":null,\"x_ray_trace_id\":null,\"correlation_id\":null},\"lambda_function\":{\"name\":null,\"arn\":null,\"memory_limit_in_m_b\":null,\"version\":null,\"cold_start\":true},\"level\":\"Information\",\"timestamp\":\"2024-01-01T00:00:00.0000000\",\"logger\":{\"name\":\"AWS.Lambda.Powertools.Logging.Logger\",\"sample_rate\":0.2}}")
            );
#endif
        }

        public void Dispose()
        {
            Logger.UseDefaultFormatter();
            Logger.RemoveAllKeys();
            LoggingLambdaContext.Clear();
            LoggingAspect.ResetForTest();
            PowertoolsLoggingSerializer.ClearOptions();
        }
    }

    [Collection("Sequential")]
    public class LogFormatterNullTest
    {
        [Fact]
        public void Log_WhenCustomFormatterReturnNull_ThrowsLogFormatException()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());
            configurations.LogLevel.Returns(LogLevel.Information.ToString());

            var logFormatter = Substitute.For<ILogFormatter>();
            logFormatter.FormatLogEntry(new LogEntry()).ReturnsNullForAnyArgs();
            Logger.UseFormatter(logFormatter);

            var systemWrapper = Substitute.For<ISystemWrapper>();
            var loggerConfiguration = new LoggerConfiguration
            {
                Service = service,
                MinimumLevel = LogLevel.Information,
                LoggerOutputCase = LoggerOutputCase.PascalCase
            };

            var provider = new LoggerProvider(loggerConfiguration, configurations, systemWrapper);
            var logger = provider.CreateLogger(loggerName);

            // Act
            void Act() => logger.LogInformation(message);

            // Assert
            Assert.Throws<LogFormatException>(Act);
            logFormatter.Received(1).FormatLogEntry(Arg.Any<LogEntry>());
            systemWrapper.DidNotReceiveWithAnyArgs().LogLine(Arg.Any<string>());

            //Clean up
            Logger.UseDefaultFormatter();
        }
    }

    [Collection("Sequential")]
    public class LogFormatterExceptionTest
    {
        [Fact]
        public void Log_WhenCustomFormatterRaisesException_ThrowsLogFormatException()
        {
            // Arrange
            var loggerName = Guid.NewGuid().ToString();
            var service = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();
            var errorMessage = Guid.NewGuid().ToString();

            var configurations = Substitute.For<IPowertoolsConfigurations>();
            configurations.Service.Returns(service);
            configurations.LoggerOutputCase.Returns(LoggerOutputCase.PascalCase.ToString());
            configurations.LogLevel.Returns(LogLevel.Information.ToString());

            var logFormatter = Substitute.For<ILogFormatter>();
            logFormatter.FormatLogEntry(new LogEntry()).ThrowsForAnyArgs(new Exception(errorMessage));
            Logger.UseFormatter(logFormatter);

            var systemWrapper = Substitute.For<ISystemWrapper>();
            var loggerConfiguration = new LoggerConfiguration
            {
                Service = service,
                MinimumLevel = LogLevel.Information,
                LoggerOutputCase = LoggerOutputCase.PascalCase
            };

            var provider = new LoggerProvider(loggerConfiguration, configurations, systemWrapper);
            var logger = provider.CreateLogger(loggerName);

            // Act
            void Act() => logger.LogInformation(message);

            // Assert
            Assert.Throws<LogFormatException>(Act);
            logFormatter.Received(1).FormatLogEntry(Arg.Any<LogEntry>());
            systemWrapper.DidNotReceiveWithAnyArgs().LogLine(Arg.Any<string>());

            //Clean up
            Logger.UseDefaultFormatter();
        }
    }
}
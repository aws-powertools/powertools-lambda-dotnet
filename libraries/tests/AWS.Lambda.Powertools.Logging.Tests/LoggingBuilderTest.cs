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
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Lambda.Powertools.Logging.Tests;

public class LoggingBuilderTest
{
    [Fact]
    public void LoggingBuilder_WhenAddPowertoolsLogger_RegistersLoggingService()
    {
        // Arrange
        var service = Guid.NewGuid().ToString();
        var message = Guid.NewGuid().ToString();
        var logLevel = LogLevel.Information;
        var loggerOutputCase = LoggerOutputCase.SnakeCase;

        var consoleOut = new StringWriter();
        Console.SetOut(consoleOut);
        
        using var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(builder =>
                builder.ClearProviders()
                    .AddPowertoolsLogger(configuration =>
                    {
                        configuration.Service = service;
                        configuration.MinimumLevel = logLevel;
                        configuration.LoggerOutputCase = loggerOutputCase;
                    }))
            .Build();
        
        var logger = host.Services.GetRequiredService<ILogger<LoggingBuilderTest>>();
        
        // Act
        logger.LogInformation(message);
        var logEntry = consoleOut.ToString();

        // Assert
        Assert.Contains($"\"service\":\"{service}\"", logEntry);
        Assert.Contains($"\"level\":\"{logLevel}\"", logEntry);
        Assert.Contains($"\"message\":\"{message}\"", logEntry);
    }
}
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
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.CloudWatchEvents.S3Events;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging.Tests.Serializers;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AWS.Lambda.Powertools.Logging.Tests.Handlers;

class TestHandlers
{
    [Logging]
    public void TestMethod()
    {
    }

    [Logging(LogLevel = LogLevel.Debug)]
    public void TestMethodDebug()
    {
    }

    [Logging(LogEvent = true)]
    public void LogEventNoArgs()
    {
    }

    [Logging(LogEvent = true, LoggerOutputCase = LoggerOutputCase.PascalCase,
        CorrelationIdPath = "/Headers/MyRequestIdHeader")]
    public void LogEvent(TestObject testObject, ILambdaContext context)
    {
    }

    [Logging(LogEvent = false)]
    public void LogEventFalse(ILambdaContext context)
    {
    }

    [Logging(LogEvent = true, LogLevel = LogLevel.Debug)]
    public void LogEventDebug()
    {
    }

    [Logging(ClearState = true)]
    public void ClearState()
    {
    }

    [Logging(CorrelationIdPath = CorrelationIdPaths.ApiGatewayRest)]
    public void CorrelationApiGatewayProxyRequest(APIGatewayProxyRequest apiGatewayProxyRequest)
    {
    }

    [Logging(CorrelationIdPath = CorrelationIdPaths.ApplicationLoadBalancer)]
    public void CorrelationApplicationLoadBalancerRequest(
        ApplicationLoadBalancerRequest applicationLoadBalancerRequest)
    {
    }

    [Logging(CorrelationIdPath = CorrelationIdPaths.EventBridge)]
    public void CorrelationCloudWatchEvent(CloudWatchEvent<S3ObjectCreate> cwEvent)
    {
    }

    [Logging(CorrelationIdPath = "/headers/my_request_id_header")]
    public void CorrelationIdFromString(TestObject testObject)
    {
    }

    [Logging(CorrelationIdPath = "/headers/my_request_id_header")]
    public void CorrelationIdFromStringSnake(TestObject testObject)
    {
    }

    [Logging(CorrelationIdPath = "/Headers/MyRequestIdHeader", LoggerOutputCase = LoggerOutputCase.PascalCase)]
    public void CorrelationIdFromStringPascal(TestObject testObject)
    {
    }

    [Logging(CorrelationIdPath = "/headers/myRequestIdHeader", LoggerOutputCase = LoggerOutputCase.CamelCase)]
    public void CorrelationIdFromStringCamel(TestObject testObject)
    {
    }

    [Logging(CorrelationIdPath = "/headers/my_request_id_header")]
    public void CorrelationIdFromStringSnakeEnv(TestObject testObject)
    {
    }

    [Logging(CorrelationIdPath = "/Headers/MyRequestIdHeader")]
    public void CorrelationIdFromStringPascalEnv(TestObject testObject)
    {
    }

    [Logging(CorrelationIdPath = "/headers/myRequestIdHeader")]
    public void CorrelationIdFromStringCamelEnv(TestObject testObject)
    {
    }

    [Logging(Service = "test", LoggerOutputCase = LoggerOutputCase.CamelCase)]
    public void HandlerService()
    {
        Logger.LogInformation("test");
    }

    [Logging(SamplingRate = 0.5, LoggerOutputCase = LoggerOutputCase.CamelCase, LogLevel = LogLevel.Information)]
    public void HandlerSamplingRate()
    {
        Logger.LogInformation("test");
    }

    [Logging(LogLevel = LogLevel.Critical)]
    public void TestLogLevelCritical()
    {
        Logger.LogCritical("test");
    }

    [Logging(LogLevel = LogLevel.Critical, LogEvent = true)]
    public void TestLogLevelCriticalLogEvent(ILambdaContext context)
    {
    }

    [Logging(LogLevel = LogLevel.Debug, LogEvent = true)]
    public void TestLogEventWithoutContext()
    {
    }

    [Logging(LogEvent = true, SamplingRate = 0.2, Service = "my_service")]
    public void TestCustomFormatterWithDecorator(string input, ILambdaContext context)
    {
    }

    [Logging(LogEvent = true, SamplingRate = 0.2, Service = "my_service")]
    public void TestCustomFormatterWithDecoratorNoContext(string input)
    {
    }

    public void TestCustomFormatterNoDecorator(string input, ILambdaContext context)
    {
        Logger.LogInformation(input);
    }

    public void TestLogNoDecorator()
    {
        Logger.LogInformation("test");
    }

    [Logging(Service = "test", LoggerOutputCase = LoggerOutputCase.SnakeCase)]
    public void TestEnums(string input, ILambdaContext context)
    {
        Logger.LogInformation(Pet.Dog);
        Logger.LogInformation(Thing.Five);
    }

    public enum Thing
    {
        One = 1,
        Three = 3,
        Five = 5
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Pet
    {
        Cat = 1,
        Dog = 3,
        Lizard = 5
    }
}

public class TestServiceHandler
{
    public void LogWithEnv()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "Environment Service");
        
        Logger.LogInformation("Service: Environment Service");
    }
    
    public void LogWithAndWithoutEnv()
    {
        Logger.LogInformation("Service: service_undefined");
        
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "Environment Service");
        
        Logger.LogInformation("Service: service_undefined");
    }

    [Logging(Service = "Attribute Service")]
    public void Handler()
    {
        Logger.LogInformation("Service: Attribute Service");
    }
}
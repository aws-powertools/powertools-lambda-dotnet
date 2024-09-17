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

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.CloudWatchEvents.S3Events;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging.Tests.Utilities;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace AWS.Lambda.Powertools.Logging.Tests.Attributes;

class TestClass
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
    
    [Logging(LogEvent = true, LoggerOutputCase = LoggerOutputCase.PascalCase)]
    public void LogEvent(ILambdaContext context)
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
        
    [Logging]
    public void HandlerServiceEnv()
    {
        Logger.LogInformation("test");
    }
        
    [Logging(SamplingRate = 0.5, LoggerOutputCase = LoggerOutputCase.CamelCase, LogLevel = LogLevel.Information)]
    public void HandlerSamplingRate()
    {
        Logger.LogInformation("test");
    }
        
    [Logging]
    public void HandlerSamplingRateEnv()
    {
        Logger.LogInformation("test");
    }
}
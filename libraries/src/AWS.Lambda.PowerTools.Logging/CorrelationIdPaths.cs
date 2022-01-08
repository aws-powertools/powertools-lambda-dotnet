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

namespace AWS.Lambda.PowerTools.Logging;

/// <summary>
///     Supported Event types from which Correlation ID can be extracted
/// </summary>
public static class CorrelationIdPaths
{
    internal const char Separator = '/';

    /// <summary>
    ///     To use when function is expecting API Gateway Rest API request event
    /// </summary>
    public const string ApiGatewayRest = "/RequestContext/RequestId";

    /// <summary>
    ///     To use when function is expecting API Gateway HTTP API request event
    /// </summary>
    public const string ApiGatewayHttp = "/RequestContext/RequestId";

    /// <summary>
    ///     To use when function is expecting Application Load balancer request event
    /// </summary>
    public const string ApplicationLoadBalancer = "/Headers/x-amzn-trace-id";

    /// <summary>
    ///     To use when function is expecting EventBridge request event
    /// </summary>
    public const string EventBridge = "/Id";
}
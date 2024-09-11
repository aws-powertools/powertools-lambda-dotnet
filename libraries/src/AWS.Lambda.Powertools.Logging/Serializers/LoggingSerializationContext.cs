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
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;

namespace AWS.Lambda.Powertools.Logging.Serializers;

#if NET8_0_OR_GREATER

/// <summary>
/// Custom JSON serializer context for AWS.Lambda.Powertools.Logging
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Int32))]
[JsonSerializable(typeof(Double))]
[JsonSerializable(typeof(DateOnly))]
[JsonSerializable(typeof(TimeOnly))]
[JsonSerializable(typeof(InvalidOperationException))]
[JsonSerializable(typeof(Exception))]
[JsonSerializable(typeof(IEnumerable<object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(Dictionary<string, int>))]
[JsonSerializable(typeof(Byte[]))]
[JsonSerializable(typeof(MemoryStream))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest.ProxyRequestContext),
    TypeInfoPropertyName = "APIGatewayProxyRequestContext")]
[JsonSerializable(typeof(APIGatewayProxyRequest.ProxyRequestClientCert),
    TypeInfoPropertyName = "APIGatewayProxyRequestProxyRequestClientCert")]
[JsonSerializable(typeof(APIGatewayProxyRequest.ClientCertValidity),
    TypeInfoPropertyName = "APIGatewayProxyRequestClientCertValidity")]
[JsonSerializable(typeof(ApplicationLoadBalancerRequest))]
[JsonSerializable(typeof(LogEntry))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext),
    TypeInfoPropertyName = "APIGatewayHttpApiV2ProxyRequestContext")]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest.ProxyRequestClientCert),
    TypeInfoPropertyName = "APIGatewayHttpApiV2ProxyRequestProxyRequestClientCert")]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest.ClientCertValidity),
    TypeInfoPropertyName = "APIGatewayHttpApiV2ProxyRequestClientCertValidity")]
public partial class PowertoolsLoggingSerializationContext : JsonSerializerContext
{
}


#endif
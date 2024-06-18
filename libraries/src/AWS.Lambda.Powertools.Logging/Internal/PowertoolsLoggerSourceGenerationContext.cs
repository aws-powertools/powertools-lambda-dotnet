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

using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.ApplicationLoadBalancerEvents;
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Logging.Internal;

#if NET8_0_OR_GREATER
/// <summary>
/// Powertools source generator that extends common generator
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ApplicationLoadBalancerRequest))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
public partial class PowertoolsLoggerSourceGenerationContext : PowertoolsSourceGenerationContext
{
}
#endif
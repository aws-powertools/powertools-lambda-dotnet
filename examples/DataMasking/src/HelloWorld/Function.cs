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
using System.Text.RegularExpressions;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.DataMasking;
using AWS.Lambda.Powertools.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace HelloWorld;

public class Function
{
    private readonly DataMasking _dataMasking = new();

    /// <summary>
    /// This example receives a JSON order payload that contains PII and demonstrates how to use the
    /// Data Masking utility to irreversibly erase sensitive fields before the payload is logged and
    /// returned. The non-sensitive fields (orderId, item, city) remain readable so the payload stays
    /// useful for debugging and monitoring.
    /// </summary>
    [Logging(LogEvent = false)] // LogEvent is intentionally off so the raw PII is never auto-logged
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var body = request.Body ?? "{}";

        // 1. Full-value masking (default *****) for whole fields
        var masked = _dataMasking.Erase(body, new[]
        {
            "customer.ssn",
            "payment.creditCard",
            "address.street"
        });

        // 2. Custom rules: keep the first char of the email, and preserve the phone length
        masked = _dataMasking.Erase(masked, new[] { "customer.email" }, new MaskingOptions
        {
            Pattern = new Regex("^(.).*@", RegexOptions.None, TimeSpan.FromSeconds(1)),
            Replacement = "$1****@"
        });

        masked = _dataMasking.Erase(masked, new[] { "customer.phone" }, new MaskingOptions
        {
            PreserveLength = true
        });

        // Logging integration: EraseToNode returns a JsonNode ready for structured logging, so the
        // masked payload is logged as a JSON object (not a string) with no extra serialization round-trip.
        // Sensitive fields are already erased, so this is safe to log.
        Logger.LogInformation(_dataMasking.EraseToNode(body, new[]
        {
            "customer.ssn",
            "customer.email",
            "customer.phone",
            "payment.creditCard",
            "address.street"
        }));

        return new APIGatewayProxyResponse
        {
            Body = masked,
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}

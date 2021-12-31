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

using Microsoft.Extensions.Logging;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    internal static class LoggingConstants
    {
        internal const LogLevel DefaultLogLevel = LogLevel.Information;
        internal const string KeyJsonFormatter = "{@json}";
        internal const string KeyColdStart = "ColdStart";
        internal const string KeyFunctionName = "FunctionName";
        internal const string KeyFunctionVersion = "FunctionVersion";
        internal const string KeyFunctionMemorySize = "FunctionMemorySize";
        internal const string KeyFunctionArn = "FunctionArn";
        internal const string KeyFunctionRequestId = "FunctionRequestId";
        internal const string KeyXRayTraceId = "XRayTraceId";
        internal const string KeyCorrelationId = "CorrelationId";
        internal const string KeyTimestamp = "Timestamp";
        internal const string KeyLogLevel = "Level";
        internal const string KeyService = "Service";
        internal const string KeyLoggerName = "Name";
        internal const string KeyMessage = "Message";
        internal const string KeySamplingRate = "SamplingRate";
        internal const string KeyException = "Exception";
    }
}
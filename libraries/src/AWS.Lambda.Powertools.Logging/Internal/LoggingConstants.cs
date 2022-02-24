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

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
///     Class LoggingConstants.
/// </summary>
internal static class LoggingConstants
{
    /// <summary>
    ///     Constant for default log level
    /// </summary>
    internal const LogLevel DefaultLogLevel = LogLevel.Information;
    
    /// <summary>
    ///     Constant for default log output case
    /// </summary>
    internal const LoggerOutputCase DefaultLoggerOutputCase = LoggerOutputCase.SnakeCase;

    /// <summary>
    ///     Constant for key json formatter
    /// </summary>
    internal const string KeyJsonFormatter = "{@json}";

    /// <summary>
    ///     Constant for key cold start
    /// </summary>
    internal const string KeyColdStart = "ColdStart";

    /// <summary>
    ///     Constant for key function name
    /// </summary>
    internal const string KeyFunctionName = "FunctionName";

    /// <summary>
    ///     Constant for key function version
    /// </summary>
    internal const string KeyFunctionVersion = "FunctionVersion";

    /// <summary>
    ///     Constant for key function memory size
    /// </summary>
    internal const string KeyFunctionMemorySize = "FunctionMemorySize";

    /// <summary>
    ///     Constant for key function arn
    /// </summary>
    internal const string KeyFunctionArn = "FunctionArn";

    /// <summary>
    ///     Constant for key function request identifier
    /// </summary>
    internal const string KeyFunctionRequestId = "FunctionRequestId";

    /// <summary>
    ///     Constant for key x ray trace identifier
    /// </summary>
    internal const string KeyXRayTraceId = "XrayTraceId";

    /// <summary>
    ///     Constant for key correlation identifier
    /// </summary>
    internal const string KeyCorrelationId = "CorrelationId";

    /// <summary>
    ///     Constant for key timestamp
    /// </summary>
    internal const string KeyTimestamp = "Timestamp";

    /// <summary>
    ///     Constant for key log level
    /// </summary>
    internal const string KeyLogLevel = "Level";

    /// <summary>
    ///     Constant for key service
    /// </summary>
    internal const string KeyService = "Service";

    /// <summary>
    ///     Constant for key logger name
    /// </summary>
    internal const string KeyLoggerName = "Name";

    /// <summary>
    ///     Constant for key message
    /// </summary>
    internal const string KeyMessage = "Message";

    /// <summary>
    ///     Constant for key sampling rate
    /// </summary>
    internal const string KeySamplingRate = "SamplingRate";

    /// <summary>
    ///     Constant for key exception
    /// </summary>
    internal const string KeyException = "Exception";
}
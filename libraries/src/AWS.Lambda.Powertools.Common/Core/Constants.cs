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

namespace AWS.Lambda.Powertools.Common;

/// <summary>
///     Class Constants
/// </summary>
internal static class Constants
{
    /// <summary>
    ///     Constant for POWERTOOLS_SERVICE_NAME environment variable
    /// </summary>
    internal const string ServiceNameEnv = "POWERTOOLS_SERVICE_NAME";

    /// <summary>
    ///     Constant for AWS_SAM_LOCAL environment variable
    /// </summary>
    internal const string SamLocalEnv = "AWS_SAM_LOCAL";

    /// <summary>
    ///     Constant for POWERTOOLS_TRACER_CAPTURE_RESPONSE environment variable
    /// </summary>
    internal const string TracerCaptureResponseEnv = "POWERTOOLS_TRACER_CAPTURE_RESPONSE";

    /// <summary>
    ///     Constant for POWERTOOLS_TRACER_CAPTURE_ERROR environment variable
    /// </summary>
    internal const string TracerCaptureErrorEnv = "POWERTOOLS_TRACER_CAPTURE_ERROR";
    
    /// <summary>
    ///     Constant for POWERTOOLS_TRACE_DISABLED environment variable
    /// </summary>
    internal const string TracingDisabledEnv = "POWERTOOLS_TRACE_DISABLED";

    /// <summary>
    ///     Constant for POWERTOOLS_METRICS_NAMESPACE environment variable
    /// </summary>
    internal const string MetricsNamespaceEnv = "POWERTOOLS_METRICS_NAMESPACE";

    /// <summary>
    ///     Constant for POWERTOOLS_LOG_LEVEL environment variable
    /// </summary>
    internal const string LogLevelNameEnv = "POWERTOOLS_LOG_LEVEL";

    /// <summary>
    ///     Constant for POWERTOOLS_LOGGER_SAMPLE_RATE environment variable
    /// </summary>
    internal const string LoggerSampleRateNameEnv = "POWERTOOLS_LOGGER_SAMPLE_RATE";

    /// <summary>
    ///     Constant for POWERTOOLS_LOGGER_LOG_EVENT environment variable
    /// </summary>
    internal const string LoggerLogEventNameEnv = "POWERTOOLS_LOGGER_LOG_EVENT";

    /// <summary>
    ///     Constant for POWERTOOLS_LOGGER_CASE environment variable
    ///     Defaults to snake case
    /// </summary>
    internal const string LoggerOutputCaseEnv = "POWERTOOLS_LOGGER_CASE";

    /// <summary>
    ///     Constant for AWS X-Ray trace identifier environment variable
    /// </summary>
    internal const string XrayTraceIdEnv = "_X_AMZN_TRACE_ID";
    
    /// <summary>
    ///     Constant for LAMBDA_TASK_ROOT environment variable
    /// </summary>
    internal const string LambdaTaskRoot = "LAMBDA_TASK_ROOT";
    
    /// <summary>
    ///     Constant for AWS_EXECUTION_ENV environment variable
    /// </summary>
    internal const string AwsExecutionEnvironmentVariableName = "AWS_EXECUTION_ENV";
    
    /// <summary>
    ///     Constant for Powertools feature identifier fo AWS_EXECUTION_ENV environment variable
    /// </summary>
    internal const string FeatureContextIdentifier = "PT";
}
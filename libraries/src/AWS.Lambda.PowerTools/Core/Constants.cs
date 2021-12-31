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

namespace AWS.Lambda.PowerTools.Core
{
    /// <summary>
    /// Class Constants
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Constant for POWERTOOLS_SERVICE_NAME environment variable 
        /// </summary>
        internal const string SERVICE_NAME_ENV = "POWERTOOLS_SERVICE_NAME";
        /// <summary>
        /// Constant for AWS_SAM_LOCAL environment variable 
        /// </summary>
        internal const string SAM_LOCAL_ENV = "AWS_SAM_LOCAL";
        /// <summary>
        /// Constant for POWERTOOLS_TRACER_CAPTURE_RESPONSE environment variable 
        /// </summary>
        internal const string TRACER_CAPTURE_RESPONSE_ENV = "POWERTOOLS_TRACER_CAPTURE_RESPONSE";
        /// <summary>
        /// Constant for POWERTOOLS_TRACER_CAPTURE_ERROR environment variable 
        /// </summary>
        internal const string TRACER_CAPTURE_ERROR_ENV = "POWERTOOLS_TRACER_CAPTURE_ERROR";
        /// <summary>
        /// Constant for POWERTOOLS_METRICS_NAMESPACE environment variable
        /// </summary>
        internal const string METRICS_NAMESPACE_ENV = "POWERTOOLS_METRICS_NAMESPACE";
        /// <summary>
        /// Constant for LOG_LEVEL environment variable 
        /// </summary>
        internal const string LOG_LEVEL_NAME_ENV = "LOG_LEVEL";
        /// <summary>
        /// Constant for POWERTOOLS_LOGGER_SAMPLE_RATE environment variable 
        /// </summary>
        internal const string LOGGER_SAMPLE_RATE_NAME_ENV = "POWERTOOLS_LOGGER_SAMPLE_RATE";
        /// <summary>
        /// Constant for POWERTOOLS_LOGGER_LOG_EVENT environment variable 
        /// </summary>
        internal const string LOGGER_LOG_EVENT_NAME_ENV = "POWERTOOLS_LOGGER_LOG_EVENT";
        /// <summary>
        /// Constant for AWS X-Ray trace identifier environment variable 
        /// </summary>
        internal const string XRAY_TRACE_ID_ENV = "_X_AMZN_TRACE_ID";
    }
}
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

namespace AWS.Lambda.PowerTools.Logging
{
    /// <summary>
    /// Supported Event types from which Correlation ID can be extracted
    /// </summary>
    public static class CorrelationIdPaths
    {
        internal const char Separator = '/';
        
        /// <summary>
        /// To use when function is expecting API Gateway Rest API Request event
        /// </summary>
        public const string API_GATEWAY_REST = "/RequestContext/RequestId";
        
        /// <summary>
        /// To use when function is expecting API Gateway HTTP API Request event
        /// </summary>
        public const string API_GATEWAY_HTTP = "/RequestContext/RequestId";
        
        /// <summary>
        /// To use when function is expecting Application Load balancer Request event
        /// </summary>
        public const string APPLICATION_LOAD_BALANCER = "/Headers/x-amzn-trace-id";
        
        /// <summary>
        /// To use when function is expecting Event Bridge Request event
        /// </summary>
        public const string EVENT_BRIDGE = "/Id";
    }
}
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
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
/// Powertools Log Entry
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Indicates the cold start.
    /// </summary>
    /// <value>The cold start value.</value>
    public bool ColdStart { get; internal set; }
    
    /// <summary>
    /// Gets the X-Ray trace identifier.
    /// </summary>
    /// <value>The X-Ray trace identifier.</value>
    public string XRayTraceId { get; internal set; }
    
    /// <summary>
    /// Gets the correlation identifier.
    /// </summary>
    /// <value>The correlation identifier.</value>
    public string CorrelationId { get; internal set; }
    
    /// <summary>
    /// Log entry timestamp in UTC.
    /// </summary>
    public DateTime Timestamp { get; internal set; }
 
    /// <summary>
    /// Log entry Level is used for logging.
    /// </summary>
    public LogLevel Level { get; internal set; }
    
    /// <summary>
    /// Service name is used for logging.
    /// </summary>
    public string Service { get; internal set; }
    
    /// <summary>
    /// Logger name is used for logging.
    /// </summary>
    public string Name { get; internal set; }
    
    /// <summary>
    /// Log entry Level is used for logging.
    /// </summary>
    public object Message { get; internal set; }
    
    /// <summary>
    /// Dynamically set a percentage of logs to DEBUG level.
    /// This can be also set using the environment variable <c>POWERTOOLS_LOGGER_SAMPLE_RATE</c>.
    /// </summary>
    /// <value>The sampling rate.</value>
    public double? SamplingRate { get; internal set; }
    
    /// <summary>
    /// Gets the appended additional keys to a log entry.
    /// <value>The extra keys.</value>
    /// </summary>
    public Dictionary<string, object> ExtraKeys { get; internal set; }
    
    /// <summary>
    /// The exception related to this entry.
    /// </summary>
    public Exception Exception { get; internal set; }
    
    /// <summary>
    /// The Lambda Context related to this entry.
    /// </summary>
    public LogEntryLambdaContext LambdaContext { get; internal set; }
}
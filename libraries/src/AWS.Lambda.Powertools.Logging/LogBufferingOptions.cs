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

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
/// Configuration options for log buffering
/// </summary>
public class LogBufferingOptions
{
    /// <summary>
    /// Gets or sets the maximum size of the buffer in bytes
    /// <para></para>
    /// Default is 20KB (20480 bytes)
    /// </summary>
    public int MaxBytes { get; set; } = 20480;
    
    /// <summary>
    /// Gets or sets the minimum log level to buffer
    /// Defaults to Debug
    /// <para></para>
    /// Valid values are: Trace, Debug, Information, Warning
    /// </summary>
    public LogLevel BufferAtLogLevel { get; set; } = LogLevel.Debug;
    
    /// <summary>
    /// Gets or sets whether to flush the buffer when logging an error
    /// </summary>
    public bool FlushOnErrorLog { get; set; } = true;
}
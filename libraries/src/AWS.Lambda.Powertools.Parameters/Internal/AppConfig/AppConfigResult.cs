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

namespace AWS.Lambda.Powertools.Parameters.Internal.AppConfig;

/// <summary>
/// AppConfigResult class.
/// </summary>
internal class AppConfigResult
{
    /// <summary>
    /// Token for polling the configuration.
    /// </summary>
    internal string PollConfigurationToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Next time poll is allowed.
    /// </summary>
    internal DateTime NextAllowedPollTime { get; set; } = DateTime.MinValue;
    
    /// <summary>
    /// Last configuration value
    /// </summary>
    internal string? LastConfig { get; set; } = null;
}

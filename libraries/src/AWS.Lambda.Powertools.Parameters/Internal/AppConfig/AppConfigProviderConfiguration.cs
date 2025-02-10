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

using AWS.Lambda.Powertools.Parameters.Configuration;

namespace AWS.Lambda.Powertools.Parameters.Internal.AppConfig;

/// <summary>
/// AppConfigProviderConfiguration class.
/// </summary>
internal class AppConfigProviderConfiguration : ParameterProviderConfiguration
{
    /// <summary>
    /// The application Id.
    /// </summary>
    internal string? ApplicationId { get; set; }
    
    /// <summary>
    /// The environment Id.
    /// </summary>
    internal string? EnvironmentId { get; set; }

    /// <summary>
    /// The configuration profile Id.
    /// </summary>
    internal string? ConfigProfileId { get; set; }
}

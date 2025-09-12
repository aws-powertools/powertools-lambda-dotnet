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
using AWS.Lambda.Powertools.Common;

namespace AWS.Lambda.Powertools.Tracing.Internal;

internal static class TracingAspectFactory
{
    /// <summary>
    /// Get an instance of the TracingAspect class.
    /// </summary>
    /// <param name="type">The type of the class to be logged.</param>
    /// <returns>An instance of the TracingAspect class.</returns>
    public static object GetInstance(Type type)
    {
        return new TracingAspect(PowertoolsConfigurations.Instance, XRayRecorder.Instance);
    }
}
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

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Singleton manager for log buffer operations with invocation context awareness
/// </summary>
internal static class LogBufferManager
{
    private static readonly List<BufferingLoggerProvider> Providers = new();
    
    /// <summary>
    /// Register a buffering provider with the manager
    /// </summary>
    internal static void RegisterProvider(BufferingLoggerProvider provider)
    {
        if (!Providers.Contains(provider))
            Providers.Add(provider);
    }
    
    /// <summary>
    /// Flush buffered logs for the current invocation
    /// </summary>
    internal static void FlushCurrentBuffer()
    {
        try
        {
            foreach (var provider in Providers)
            {
                provider?.FlushBuffers();
            }
        }
        catch (Exception)
        {
            // Suppress errors
        }
    }
    
    /// <summary>
    /// Clear buffered logs for the current invocation
    /// </summary>
    internal static void ClearCurrentBuffer()
    {
        try
        {
            foreach (var provider in Providers)
            {
                provider?.ClearCurrentBuffer();
            }
        }
        catch (Exception)
        {
            // Suppress errors
        }
    }
    
    /// <summary>
    /// Unregister a buffering provider from the manager
    /// </summary>
    /// <param name="provider"></param>
    internal static void UnregisterProvider(BufferingLoggerProvider provider)
    {
        Providers.Remove(provider);
    }
    
    /// <summary>
    /// Reset the manager state (for testing purposes)
    /// </summary>
    internal static void ResetForTesting()
    {
        Providers.Clear();
    }
}
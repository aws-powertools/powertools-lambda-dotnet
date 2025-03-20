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
using System.Collections.Concurrent;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Singleton manager for log buffer operations
/// </summary>
internal static class LogBufferManager
{
    private static BufferingLoggerProvider _provider;
    
    /// <summary>
    /// Register a buffering provider with the manager
    /// </summary>
    internal static void RegisterProvider(BufferingLoggerProvider provider)
    {
        _provider = provider;
    }
    
    /// <summary>
    /// Flush all buffered logs
    /// </summary>
    internal static void FlushAllBuffers()
    {
        try
        {
            _provider?.FlushBuffers();
        }
        catch (Exception)
        {
            // Suppress errors
        }
    }
    
    /// <summary>
    /// Clear all buffered logs
    /// </summary>
    internal static void ClearAllBuffers()
    {
        try
        {
            _provider?.ClearBuffers();
        }
        catch (Exception)
        {
            // Suppress errors
        }
    }
}
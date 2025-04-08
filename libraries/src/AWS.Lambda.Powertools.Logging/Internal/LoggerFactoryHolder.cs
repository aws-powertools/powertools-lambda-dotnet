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
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging.Internal;

/// <summary>
/// Holds and manages the shared logger factory instance
/// </summary>
internal static class LoggerFactoryHolder
{
    private static ILoggerFactory _factory;
    private static readonly object _lock = new object();
    
    /// <summary>
    /// Gets or creates the shared logger factory
    /// </summary>
    public static ILoggerFactory GetOrCreateFactory()
    {
        lock (_lock)
        {
            if (_factory == null)
            {
                var config = PowertoolsLoggingBuilderExtensions.GetCurrentConfiguration();
                
                _factory = LoggerFactoryHelper.CreateAndConfigureFactory(config);
            }
            return _factory;
        }
    }

    public static void SetFactory(ILoggerFactory factory)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        lock (_lock)
        {
            _factory = factory;
            Logger.ClearInstance();
        }
    }
    
    /// <summary>
    /// Resets the factory holder for testing
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            // Dispose the old factory if it exists
            if (_factory == null) return;
            try
            {
                _factory.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
                
            _factory = null;
        }
    }
}
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
using System.Text.Json;
using AWS.Lambda.Powertools.Common;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class Logger.
/// </summary>
public static partial class Logger
{
    private static ILogger _loggerInstance;
    private static readonly object Lock = new object();

    // Change this to a property with getter that recreates if needed
    private static ILogger LoggerInstance 
    {
        get
        {
            // If we have no instance or configuration has changed, get a new logger
            if (_loggerInstance == null)
            {
                lock (Lock)
                {
                    if (_loggerInstance == null)
                    {
                        _loggerInstance = Initialize();
                    }
                }
            }
            return _loggerInstance;
        }
    }

    private static ILogger Initialize()
    {
        return LoggerFactoryHolder.GetOrCreateFactory().CreatePowertoolsLogger();
    }

    /// <summary>
    /// Configure with an existing logger factory
    /// </summary>
    /// <param name="loggerFactory">The factory to use</param>
    internal static void Configure(ILoggerFactory loggerFactory)
    {
        if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
        LoggerFactoryHolder.SetFactory(loggerFactory);
    }

    /// <summary>
    /// Configure using a configuration action
    /// </summary>
    /// <param name="configure"></param>
    public static void Configure(Action<PowertoolsLoggerConfiguration> configure)
    {
        lock (Lock)
        {
            var config = new PowertoolsLoggerConfiguration();
            configure(config);
            _loggerInstance = LoggerFactoryHelper.CreateAndConfigureFactory(config).CreatePowertoolsLogger();
        }
    }
    
    
    /// <summary>
    /// Reset the logger for testing
    /// </summary>
    internal static void Reset()
    {
        LoggerFactoryHolder.Reset();
        _loggerInstance = null;
        RemoveAllKeys();
    }
    
    internal static void ClearInstance()
    {
        _loggerInstance = null;
    }
}
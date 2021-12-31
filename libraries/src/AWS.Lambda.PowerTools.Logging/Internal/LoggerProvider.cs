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

using System.Collections.Concurrent;
using AWS.Lambda.PowerTools.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWS.Lambda.PowerTools.Logging.Internal
{
    internal sealed class LoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, PowerToolsLogger> _loggers = new();

        internal LoggerProvider(IOptions<LoggerConfiguration> config)
        {
            _currentConfig = config?.Value;
        }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName,
                name => new PowerToolsLogger(name, 
                    PowerToolsConfigurations.Instance,
                    SystemWrapper.Instance,
                    GetCurrentConfig));

        
        private LoggerConfiguration _currentConfig;
        private LoggerConfiguration GetCurrentConfig() => _currentConfig;

        public void Dispose()
        {
            _loggers.Clear();
        }

        internal void Configure(IOptions<LoggerConfiguration> config)
        {
            if (_currentConfig is not null || config is null)
                return;
            
            _currentConfig = config.Value;
            foreach (var logger in _loggers.Values)
                logger.ClearConfig();
        }
    }
}
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
using System.Linq;
using AWS.Lambda.Powertools.Logging.Internal;
using AWS.Lambda.Powertools.Logging.Internal.Helpers;
using Microsoft.Extensions.Logging;

namespace AWS.Lambda.Powertools.Logging;

/// <summary>
///     Class Logger.
/// </summary>
public partial class Logger : ILogger
{
    /// <summary>
    ///     The logger instance
    /// </summary>
    private static ILogger _loggerInstance;

    /// <summary>
    ///     Gets the logger instance.
    /// </summary>
    /// <value>The logger instance.</value>
    private static ILogger LoggerInstance => _loggerInstance ??= Create<Logger>();

    /// <summary>
    ///     Gets or sets the logger provider.
    /// </summary>
    /// <value>The logger provider.</value>
    internal static ILoggerProvider LoggerProvider { get; set; }

    /// <summary>
    ///     The logger formatter instance
    /// </summary>
    private static ILogFormatter _logFormatter;

    /// <summary>
    ///     Gets the scope.
    /// </summary>
    /// <value>The scope.</value>
    private static IDictionary<string, object> Scope { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    ///     Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    /// <exception cref="System.ArgumentNullException">categoryName</exception>
    public static ILogger Create(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentNullException(nameof(categoryName));

        // Needed for when using Logger directly with decorator
        LoggerProvider ??= new LoggerProvider(null);

        return LoggerProvider.CreateLogger(categoryName);
    }

    /// <summary>
    ///     Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>The instance of <see cref="T:Microsoft.Extensions.Logging.ILogger" /> that was created.</returns>
    public static ILogger Create<T>()
    {
        return Create(typeof(T).FullName);
    }

    internal static void ClearLoggerInstance()
    {
        _loggerInstance = null;
    }

    #region ILogger Interface Implementation

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <typeparam name="TState">The type of state to begin scope for.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose.</returns>
    public IDisposable BeginScope<TState>(TState state)
    {
        return LoggerInstance.BeginScope(state);
    }

    /// <summary>
    /// Checks if the given <paramref name="logLevel"/> is enabled.
    /// </summary>
    /// <param name="logLevel">Level to be checked.</param>
    /// <returns><c>true</c> if enabled.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return LoggerInstance.IsEnabled(logLevel);
    }

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">Id of the event.</param>
    /// <param name="state">The entry to be written. Can be also an object.</param>
    /// <param name="exception">The exception related to this entry.</param>
    /// <param name="formatter">
    ///     Function to create a <see cref="T:System.String" /> message of the <paramref name="state" />
    ///     and <paramref name="exception" />.
    /// </param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        LoggerInstance.Log(logLevel, eventId, state, exception, formatter);
    }

    #endregion
}
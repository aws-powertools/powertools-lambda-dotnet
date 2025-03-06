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
using AWS.Lambda.Powertools.Logging.Internal.Helpers;

namespace AWS.Lambda.Powertools.Logging;

public partial class Logger
{
    #region Scope Variables

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="System.ArgumentNullException">key</exception>
    /// <exception cref="System.ArgumentNullException">value</exception>
    public static void AppendKey(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));
            
#if NET8_0_OR_GREATER
        Scope[key] = PowertoolsLoggerHelpers.ObjectToDictionary(value) ??
                     throw new ArgumentNullException(nameof(value));
#else
        Scope[key] = value ?? throw new ArgumentNullException(nameof(value));
#endif
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(IEnumerable<KeyValuePair<string, object>> keys)
    {
        foreach (var (key, value) in keys)
            AppendKey(key, value);
    }

    /// <summary>
    ///     Appending additional key to the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void AppendKeys(IEnumerable<KeyValuePair<string, string>> keys)
    {
        foreach (var (key, value) in keys)
            AppendKey(key, value);
    }

    /// <summary>
    ///     Remove additional keys from the log context.
    /// </summary>
    /// <param name="keys">The list of keys.</param>
    public static void RemoveKeys(params string[] keys)
    {
        if (keys == null) return;
        foreach (var key in keys)
            if (Scope.ContainsKey(key))
                Scope.Remove(key);
    }

    /// <summary>
    ///     Returns all additional keys added to the log context.
    /// </summary>
    /// <returns>IEnumerable&lt;KeyValuePair&lt;System.String, System.Object&gt;&gt;.</returns>
    public static IEnumerable<KeyValuePair<string, object>> GetAllKeys()
    {
        return Scope.AsEnumerable();
    }

    /// <summary>
    ///     Removes all additional keys from the log context.
    /// </summary>
    internal static void RemoveAllKeys()
    {
        Scope.Clear();
    }

    #endregion
}

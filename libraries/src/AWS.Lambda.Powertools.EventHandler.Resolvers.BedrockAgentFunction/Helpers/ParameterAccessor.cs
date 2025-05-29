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

using System.Globalization;

namespace AWS.Lambda.Powertools.EventHandler.Resolvers.BedrockAgentFunction.Helpers;

/// <summary>
/// Provides strongly-typed access to the parameters of an agent function call.
/// </summary>
internal class ParameterAccessor
{
    private readonly List<Parameter> _parameters;

    internal ParameterAccessor(List<Parameter>? parameters)
    {
        _parameters = parameters ?? new List<Parameter>();
    }

    /// <summary>
    /// Gets a parameter value by name with type conversion.
    /// </summary>
    public T Get<T>(string name)
    {
        var parameter = _parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (parameter == null || parameter.Value == null)
        {
            return default!;
        }

        return ConvertParameter<T>(parameter);
    }

    /// <summary>
    /// Gets a parameter value by index with type conversion.
    /// </summary>
    public T GetAt<T>(int index)
    {
        if (index < 0 || index >= _parameters.Count)
        {
            return default!;
        }

        var parameter = _parameters[index];
        if (parameter.Value == null)
        {
            return default!;
        }

        return ConvertParameter<T>(parameter);
    }

    /// <summary>
    /// Gets a parameter value by name with fallback to a default value.
    /// </summary>
    public T GetOrDefault<T>(string name, T defaultValue)
    {
        var parameter = _parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (parameter == null || parameter.Value == null)
        {
            return defaultValue;
        }

        try
        {
            var result = ConvertParameter<T>(parameter);
            // If conversion returns default value but we have a non-null parameter,
            // that means conversion failed, so return the provided default value
            if (EqualityComparer<T>.Default.Equals(result, default) && parameter.Value != null)
            {
                return defaultValue;
            }
            return result;
        }
        catch
        {
            return defaultValue;
        }
    }

    private static T ConvertParameter<T>(Parameter? parameter)
    {
        if (parameter == null || parameter.Value == null)
        {
            return default!;
        }

        // Handle different types explicitly for AOT compatibility
        if (typeof(T) == typeof(string))
        {
            return (T)(object)parameter.Value;
        }

        if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
        {
            if (int.TryParse(parameter.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
            {
                return (T)(object)result;
            }
            return default!;
        }

        if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
        {
            if (double.TryParse(parameter.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return (T)(object)result;
            }
            return default!;
        }

        if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
        {
            if (bool.TryParse(parameter.Value, out bool result))
            {
                return (T)(object)result;
            }
            return default!;
        }

        if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
        {
            if (long.TryParse(parameter.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out long result))
            {
                return (T)(object)result;
            }
            return default!;
        }

        if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
        {
            if (decimal.TryParse(parameter.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return (T)(object)result;
            }
            return default!;
        }

        // Return default for array and complex types
        return default!;
    }
}

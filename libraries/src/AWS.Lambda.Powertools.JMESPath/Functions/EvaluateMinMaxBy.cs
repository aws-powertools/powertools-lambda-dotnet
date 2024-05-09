/*
 * Copyright JsonCons.Net authors. All Rights Reserved.
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

using System.Collections.Generic;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Operators;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Evaluates the min and max functions by resources.
/// </summary>
internal static class EvaluateMinMaxBy
{
    /// <summary>
    /// Evaluates the min and max function by resources.
    /// </summary>
    /// <param name="resources"></param>
    /// <param name="args"></param>
    /// <param name="binaryOperator"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    internal static bool TryEvaluate(DynamicResources resources, IList<IValue> args, IBinaryOperator binaryOperator, out IValue element)
    {
        if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
        {
            element = JsonConstants.Null;
            return false;
        }

        var arg0 = args[0];
        if (arg0.GetArrayLength() == 0)
        {
            element = JsonConstants.Null;
            return true;
        }

        var expr = args[1].GetExpression();

        if (!expr.TryEvaluate(resources, arg0[0], out var key1))
        {
            element = JsonConstants.Null;
            return false;
        }

        var isNumber1 = key1.Type == JmesPathType.Number;
        var isString1 = key1.Type == JmesPathType.String;
        if (!(isNumber1 || isString1))
        {
            element = JsonConstants.Null;
            return false;
        }

        var index = 0;
        for (var i = 1; i < arg0.GetArrayLength(); ++i)
        {
            if (!expr.TryEvaluate(resources, arg0[i], out var key2))
            {
                element = JsonConstants.Null;
                return false;
            }

            var isNumber2 = key2.Type == JmesPathType.Number;
            var isString2 = key2.Type == JmesPathType.String;
            if (!(isNumber2 == isNumber1 && isString2 == isString1))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (!binaryOperator.TryEvaluate(key2, key1, out var value))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (value.Type != JmesPathType.True) continue;
            key1 = key2;
            index = i;
        }

        element = arg0[index];
        return true;
    }
}
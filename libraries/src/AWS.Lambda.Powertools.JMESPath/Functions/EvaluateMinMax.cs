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

using System.Collections.Generic;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal static class EvaluateMinMax
{
    internal static bool TryEvaluate(IList<IValue> args, IBinaryOperator binaryOperator, out IValue element)
    {
        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Array)
        {
            element = JsonConstants.Null;
            return false;
        }

        if (arg0.GetArrayLength() == 0)
        {
            element = JsonConstants.Null;
            return false;
        }

        var isNumber = arg0[0].Type == JmesPathType.Number;
        var isString = arg0[0].Type == JmesPathType.String;
        if (!isNumber && !isString)
        {
            element = JsonConstants.Null;
            return false;
        }

        var index = 0;
        for (var i = 1; i < arg0.GetArrayLength(); ++i)
        {
            if (!(arg0[i].Type == JmesPathType.Number == isNumber &&
                  arg0[i].Type == JmesPathType.String == isString))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (!binaryOperator.TryEvaluate(arg0[i], arg0[index], out var value))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (Expression.IsTrue(value))
            {
                index = i;
            }
        }

        element = arg0[index];
        return true;
    }
}
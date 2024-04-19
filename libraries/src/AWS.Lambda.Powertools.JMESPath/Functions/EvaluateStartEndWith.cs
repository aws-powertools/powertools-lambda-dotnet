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

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal static class EvaluateStartEndWith
{
    internal static bool TryEvaluate(IList<IValue> args, out IValue element, Func<string, Func<string, bool>> method)
    {
        var arg0 = args[0];
        var arg1 = args[1];
        if (arg0.Type != JmesPathType.String
            || arg1.Type != JmesPathType.String)
        {
            element = JsonConstants.Null;
            return false;
        }

        var s0 = arg0.GetString();
        var s1 = arg1.GetString();
        element = method(s0)(s1) ? JsonConstants.True : JsonConstants.False;

        return true;
    }
}
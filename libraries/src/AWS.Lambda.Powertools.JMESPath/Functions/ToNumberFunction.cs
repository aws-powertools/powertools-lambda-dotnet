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
using System.Diagnostics;
using System.Globalization;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class ToNumberFunction : BaseFunction
{
    internal ToNumberFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        switch (arg0.Type)
        {
            case JmesPathType.Number:
                element = arg0;
                return true;
            case JmesPathType.String:
            {
                var s = arg0.GetString();
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                {
                    element = new DecimalValue(dec);
                    return true;
                }

                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl))
                {
                    element = new DoubleValue(dbl);
                    return true;
                }

                element = JsonConstants.Null;
                return false;
            }
            default:
                element = JsonConstants.Null;
                return false;
        }
    }

    public override string ToString()
    {
        return "to_number";
    }
}
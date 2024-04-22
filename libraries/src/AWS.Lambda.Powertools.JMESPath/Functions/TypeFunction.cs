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
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// Returns the type of the value as a string.
/// </summary>
internal sealed class TypeFunction : BaseFunction
{
    internal TypeFunction()
        : base(1)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];

        switch (arg0.Type)
        {
            case JmesPathType.Number:
                element = new StringValue("number");
                return true;
            case JmesPathType.True:
            case JmesPathType.False:
                element = new StringValue("boolean");
                return true;
            case JmesPathType.String:
                element = new StringValue("string");
                return true;
            case JmesPathType.Object:
                element = new StringValue("object");
                return true;
            case JmesPathType.Array:
                element = new StringValue("array");
                return true;
            case JmesPathType.Null:
                element = new StringValue("null");
                return true;
            default:
                element = JsonConstants.Null;
                return false;
        }
    }

    public override string ToString()
    {
        return "type";
    }
}
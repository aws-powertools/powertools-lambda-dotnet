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

using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Operators;

/// <summary>
/// Base class for all binary operators that are comparison
/// </summary>
internal sealed class GtOperator : BinaryOperator
{
    /// <summary>
    /// Singleton instance of the <see cref="GtOperator"/> class
    /// </summary>
    internal static GtOperator Instance { get; } = new();

    private GtOperator()
        : base(Operator.Gt)
    {
    }

    /// <inheritdoc />
    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        switch (lhs.Type)
        {
            case JmesPathType.Number when rhs.Type == JmesPathType.Number:
            {
                if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
                {
                    result = dec1 > dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
                {
                    result = val1 > val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }

                break;
            }
            case JmesPathType.String when rhs.Type == JmesPathType.String:
                result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) > 0 ? JsonConstants.True : JsonConstants.False;
                break;
            default:
                result = JsonConstants.Null;
                break;
        }

        return true;
    }

    public override string ToString()
    {
        return "GtOperator";
    }
}
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

using System.Text.RegularExpressions;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Operators;

internal sealed class RegexOperator : UnaryOperator
{
    private readonly Regex _regex;

    internal RegexOperator(Regex regex)
        : base(Operator.Not)
    {
        _regex = regex;
    }

    public override bool TryEvaluate(IValue elem, out IValue result)
    {
        if (elem.Type != JmesPathType.String)
        {
            result = JsonConstants.Null;
            return false; // type error
        }
        result = _regex.IsMatch(elem.GetString()) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "Regex";
    }
}
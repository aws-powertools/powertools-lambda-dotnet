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
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions
{
    /// <summary>
    /// Implements the <code>sort_by</code> function.
    /// </summary>
    internal sealed class SortByComparer : IComparer<IValue>, System.Collections.IComparer
    {
        private readonly DynamicResources _resources;
        private readonly IExpression _expr;

        internal bool IsValid { get; private set; } = true;

        internal SortByComparer(DynamicResources resources,
            IExpression expr)
        {
            _resources = resources;
            _expr = expr;
        }

        public int Compare(IValue lhs, IValue rhs)
        {
            var comparer = ValueComparer.Instance;

            if (!IsValid)
            {
                return 0;
            }

            if (!_expr.TryEvaluate(_resources, lhs, out var key1))
            {
                IsValid = false;
                return 0;
            }

            var isNumber1 = key1.Type == JmesPathType.Number;
            var isString1 = key1.Type == JmesPathType.String;
            if (!(isNumber1 || isString1))
            {
                IsValid = false;
                return 0;
            }

            if (!_expr.TryEvaluate(_resources, rhs, out var key2))
            {
                IsValid = false;
                return 0;
            }

            var isNumber2 = key2.Type == JmesPathType.Number;
            var isString2 = key2.Type == JmesPathType.String;
            if (isNumber2 == isNumber1 && isString2 == isString1) return comparer.Compare(key1, key2);
            IsValid = false;
            return 0;
        }

        int System.Collections.IComparer.Compare(object x, object y)
        {
            return Compare((IValue)x, (IValue)y);
        }
    }
}
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

using System.Text.RegularExpressions;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal interface IUnaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        bool TryEvaluate(IValue elem, out IValue result);
    }

    internal abstract class UnaryOperator : IUnaryOperator
    {
        private protected UnaryOperator(Operator oper)
        {
            PrecedenceLevel = OperatorTable.PrecedenceLevel(oper);
            IsRightAssociative = OperatorTable.IsRightAssociative(oper);
        }

        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public abstract bool TryEvaluate(IValue elem, out IValue result);
    }

    internal sealed class NotOperator : UnaryOperator
    {
        internal static NotOperator Instance { get; } = new();

        private NotOperator()
            : base(Operator.Not)
        {}

        public override bool TryEvaluate(IValue val, out IValue result)
        {
            result = Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "Not";
        }
    }

    internal sealed class RegexOperator : UnaryOperator
    {
        private readonly Regex _regex;

        internal RegexOperator(Regex regex)
            : base(Operator.Not)
        {
            _regex = regex;
        }

        public override bool TryEvaluate(IValue val, out IValue result)
        {
            if (val.Type != JmesPathType.String)
            {
                result = JsonConstants.Null;
                return false; // type error
            }
            result = _regex.IsMatch(val.GetString()) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "Regex";
        }
    }
}


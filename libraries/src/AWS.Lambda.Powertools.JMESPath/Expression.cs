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
using System.Linq;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal static class JsonConstants
    {
        static JsonConstants()
        {
            True = new TrueValue();
            False = new FalseValue();
            Null = new NullValue();
        }

        internal static IValue True {get;}
        internal static IValue False {get;}
        internal static IValue Null {get;}
    }

    internal interface IExpression
    {
         bool TryEvaluate(DynamicResources resources,
                          IValue current, 
                          out IValue value);

         int PrecedenceLevel {get;} 

         bool IsProjection {get;} 

         bool IsRightAssociative {get;}

        void AddExpression(IExpression expr);
    }

    // BaseExpression
    internal abstract class BaseExpression : IExpression
    {
        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public bool IsProjection {get;}

        private protected BaseExpression(Operator oper, bool isProjection)
        {
            PrecedenceLevel = OperatorTable.PrecedenceLevel(oper);
            IsRightAssociative = OperatorTable.IsRightAssociative(oper);
            IsProjection = isProjection;
        }

        public abstract bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value);

        public virtual void AddExpression(IExpression expr)
        {
        }

        public override string ToString()
        {
            return "ToString not implemented";
        }
    }

    internal sealed class IdentifierSelector : BaseExpression
    {
        private readonly string _identifier;
    
        internal IdentifierSelector(string name)
            : base(Operator.Default, false)
        {
            _identifier = name;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type == JmesPathType.Object && current.TryGetProperty(_identifier, out value))
            {
                return true;
            }

            value = JsonConstants.Null;
            return true;
        }

        public override string ToString()
        {
            return $"IdentifierSelector {_identifier}";
        }
    }

    internal sealed class CurrentNode : BaseExpression
    {
        internal CurrentNode()
            : base(Operator.Default, false)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            value = current;
            return true;
        }

        public override string ToString()
        {
            return "CurrentNode";
        }
    }

    internal sealed class IndexSelector : BaseExpression
    {
        private readonly int _index;
        internal IndexSelector(int index)
            : base(Operator.Default, false)
        {
            _index = index;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }
            var slen = current.GetArrayLength();
            if (_index >= 0 && _index < slen)
            {
                value = current[_index];
            }
            else if ((slen + _index) >= 0 && (slen+_index) < slen)
            {
                var index = slen + _index;
                value = current[index];
            }
            else
            {
                value = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return $"Index Selector {_index}";
        }
    }

    internal abstract class Projection : BaseExpression
    {
        private readonly List<IExpression> _expressions;

        private protected Projection(Operator oper)
            : base(oper, true)
        {
            _expressions = new List<IExpression>();
        }

        public override void AddExpression(IExpression expr)
        {
            if (_expressions.Count != 0 && _expressions[_expressions.Count-1].IsProjection && 
                (expr.PrecedenceLevel > _expressions[_expressions.Count-1].PrecedenceLevel ||
                 (expr.PrecedenceLevel == _expressions[_expressions.Count-1].PrecedenceLevel && expr.IsRightAssociative)))
            {
                _expressions[_expressions.Count-1].AddExpression(expr);
            }
            else
            {
                _expressions.Add(expr);
            }
        }
        internal bool TryApplyExpressions(DynamicResources resources, IValue current, out IValue value)
        {
            value = current;
            foreach (var expression in _expressions)
            {
                if (!expression.TryEvaluate(resources, value, out value))
                {
                    return false;
                }
            }
            return true;
        }
    }

    internal sealed class ObjectProjection : Projection
    {
        internal ObjectProjection()
            : base(Operator.Projection)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Object)
            {
                value = JsonConstants.Null;
                return true;
            }

            var result = new List<IValue>();
            value = new ArrayValue(result);
            foreach (var item in current.EnumerateObject())
            {
                if (item.Value.Type == JmesPathType.Null) continue;
                if (!TryApplyExpressions(resources, item.Value, out var val))
                {
                    return false;
                }
                if (val.Type != JmesPathType.Null)
                {
                    result.Add(val);
                }
            }
            return true;
        }

        public override string ToString()
        {
            return "ObjectProjection";
        }
    }

    internal sealed class ListProjection : Projection
    {    
        internal ListProjection()
            : base(Operator.Projection)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }

            var result = new List<IValue>();
            foreach (var item in current.EnumerateArray())
            {
                if (item.Type != JmesPathType.Null)
                {
                    if (!TryApplyExpressions(resources, item, out var val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "ListProjection";
        }
    }

    internal sealed class FlattenProjection : Projection
    {
        internal FlattenProjection()
            : base(Operator.FlattenProjection)
        {
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }

            var result = new List<IValue>();
            foreach (var item in current.EnumerateArray())
            {
                if (item.Type == JmesPathType.Array)
                {
                    foreach (var elem in item.EnumerateArray())
                    {
                        if (elem.Type == JmesPathType.Null) continue;
                        if (!TryApplyExpressions(resources, elem, out var val))
                        {
                            value = JsonConstants.Null;
                            return false;
                        }
                        if (val.Type != JmesPathType.Null)
                        {
                            result.Add(val);
                        }
                    }
                }
                else
                {
                    if (item.Type == JmesPathType.Null) continue;
                    if (!TryApplyExpressions(resources, item, out var val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }

            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "FlattenProjection";
        }
    }

    internal sealed class SliceProjection : Projection
    {
        private readonly Slice _slice;
    
        internal SliceProjection(Slice s)
            : base(Operator.Projection)
        {
            _slice = s;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }

            var start = _slice.GetStart(current.GetArrayLength());
            var end = _slice.GetStop(current.GetArrayLength());
            var step = _slice.Step;

            if (step == 0)
            {
                value = JsonConstants.Null;
                return false;
            }

            var result = new List<IValue>();
            if (step > 0)
            {
                if (start < 0)
                {
                    start = 0;
                }
                if (end > current.GetArrayLength())
                {
                    end = current.GetArrayLength();
                }
                for (var i = start; i < end; i += step)
                {
                    if (!TryApplyExpressions(resources, current[i], out var val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }
            else
            {
                if (start >= current.GetArrayLength())
                {
                    start = current.GetArrayLength() - 1;
                }
                if (end < -1)
                {
                    end = -1;
                }
                for (var i = start; i > end; i += step)
                {
                    if (!TryApplyExpressions(resources, current[i], out var val))
                    {
                        value = JsonConstants.Null;
                        return false;
                    }
                    if (val.Type != JmesPathType.Null)
                    {
                        result.Add(val);
                    }
                }
            }

            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "SliceProjection";
        }
    }

    internal sealed class FilterExpression : Projection
    {
        private readonly Expression _expr;
    
        internal FilterExpression(Expression expr)
            : base(Operator.Projection)
        {
            _expr = expr;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type != JmesPathType.Array)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new List<IValue>();

            foreach (var item in current.EnumerateArray())
            {
                if (!_expr.TryEvaluate(resources, item, out var test))
                {
                    value = JsonConstants.Null;
                    return false;
                }

                if (!Expression.IsTrue(test)) continue;
                if (!TryApplyExpressions(resources, item, out var val))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                if (val.Type != JmesPathType.Null)
                {
                    result.Add(val);
                }
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "FilterExpression";
        }
    }

    internal sealed class MultiSelectList : BaseExpression
    {
        private readonly IList<Expression> _expressions;
    
        internal MultiSelectList(IList<Expression> expressions)
            : base(Operator.Default, false)
        {
            _expressions = expressions;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type == JmesPathType.Null)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new List<IValue>();

            foreach (var expr in _expressions)
            {
                if (!expr.TryEvaluate(resources, current, out var val))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                result.Add(val);
            }
            value = new ArrayValue(result);
            return true;
        }

        public override string ToString()
        {
            return "MultiSelectList";
        }
    }

    internal struct KeyExpressionPair
    {
        internal string Key {get;}
        internal Expression Expression {get;}

        internal KeyExpressionPair(string key, Expression expression) 
        {
            Key = key;
            Expression = expression;
        }
    }

    internal sealed class MultiSelectHash : BaseExpression
    {
        private readonly IList<KeyExpressionPair> _keyExprPairs;

        internal MultiSelectHash(IList<KeyExpressionPair> keyExprPairs)
            : base(Operator.Default, false)
        {
            _keyExprPairs = keyExprPairs;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (current.Type == JmesPathType.Null)
            {
                value = JsonConstants.Null;
                return true;
            }
            var result = new Dictionary<string,IValue>();
            foreach (var item in _keyExprPairs)
            {
                if (!item.Expression.TryEvaluate(resources, current, out var val))
                {
                    value = JsonConstants.Null;
                    return false;
                }
                result.Add(item.Key, val);
            }

            value = new ObjectValue(result);
            return true;
        }

        public override string ToString()
        {
            return "MultiSelectHash";
        }
    }

    internal sealed class FunctionExpression : BaseExpression
    {
        private readonly Expression _expr;

        internal FunctionExpression(Expression expr)
            : base(Operator.Default, false)
        {
            _expr = expr;
        }

        public override bool TryEvaluate(DynamicResources resources,
                                         IValue current, 
                                         out IValue value)
        {
            if (!_expr.TryEvaluate(resources, current, out var val))
            {
                value = JsonConstants.Null;
                return true;
            }
            value = val;
            return true;
        }

        public override string ToString()
        {
            return "FunctionExpression";
        }
    }

    internal class Expression
    {
        private readonly Token[] _tokens;

        internal Expression(Token[] tokens)
        {
            _tokens = tokens;
        }

        public  bool TryEvaluate(DynamicResources resources,
                                 IValue current, 
                                 out IValue result)
        {
            var stack = new Stack<IValue>();
            IList<IValue> argStack = new List<IValue>();

            var rootPtr = current;

            for (var i = _tokens.Length-1; i >= 0; --i)
            {
                var token = _tokens[i];
                switch (token.Type)
                {
                    case TokenType.Literal:
                    {
                        stack.Push(token.GetValue());
                        break;
                    }
                    case TokenType.BeginExpressionType:
                    {
                        Debug.Assert(i>0);
                        token = _tokens[--i];
                        Debug.Assert(token.Type == TokenType.Expression);
                        Debug.Assert(stack.Count != 0);
                        stack.Pop();
                        stack.Push(new ExpressionValue(token.GetExpression()));
                        break;
                    }
                    case TokenType.Pipe:
                    {
                        Debug.Assert(stack.Count != 0);
                        rootPtr = stack.Peek();
                        break;
                    }
                    case TokenType.CurrentNode:
                        stack.Push(rootPtr);
                        break;
                    case TokenType.Expression:
                    {
                        Debug.Assert(stack.Count != 0);
                        var ptr = stack.Pop();
                        if (!token.GetExpression().TryEvaluate(resources, ptr, out var val))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(val);
                        break;
                    }
                    case TokenType.UnaryOperator:
                    {
                        Debug.Assert(stack.Count >= 1);
                        var rhs = stack.Pop();
                        if (!token.GetUnaryOperator().TryEvaluate(rhs, out var val))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(val);
                        break;
                    }
                    case TokenType.BinaryOperator:
                    {
                        Debug.Assert(stack.Count >= 2);
                        var rhs = stack.Pop();
                        var lhs = stack.Pop();
                        if (!token.GetBinaryOperator().TryEvaluate(lhs, rhs, out var val))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        stack.Push(val);
                        break;
                    }
                    case TokenType.Argument:
                    {
                        Debug.Assert(stack.Count != 0);
                        argStack.Add(stack.Pop());
                        break;
                    }
                    case TokenType.Function:
                    {
                        if (token.GetFunction().Arity != null && token.GetFunction().Arity != argStack.Count())
                        {
                            // airty error should never happen here
                            result = JsonConstants.Null;
                            return false;
                        }

                        if (!token.GetFunction().TryEvaluate(resources, argStack, out var val))
                        {
                            result = JsonConstants.Null;
                            return false;
                        }
                        argStack.Clear();
                        stack.Push(val);
                        break;
                    }
                    default:
                        break;
                }
            }
            Debug.Assert(stack.Count == 1);
            result = stack.Peek();
            return true;
        }

        internal static bool IsFalse(IValue val)
        {
            switch (val.Type)
            {
                case JmesPathType.False:
                    return true;
                case JmesPathType.Null:
                    return true;
                case JmesPathType.Array:
                    return val.GetArrayLength() == 0;
                case JmesPathType.Object:
                    return val.EnumerateObject().MoveNext() == false;
                case JmesPathType.String:
                    return val.GetString().Length == 0;
                case JmesPathType.Number:
                    return false;
                default:
                    return false;
            }
        }

        internal static bool IsTrue(IValue val)
        {
            return !IsFalse(val);
        }
    }
}


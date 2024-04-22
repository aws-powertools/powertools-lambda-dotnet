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

namespace AWS.Lambda.Powertools.JMESPath.Expressions
{
    // BaseExpression

    /// <summary>
    /// Represents a JMESPath expression.
    /// </summary>
    internal class Expression
    {
        /// <summary>
        /// The tokens in the expression.
        /// </summary>
        private readonly Token[] _tokens;

        internal Expression(Token[] tokens)
        {
            _tokens = tokens;
        }

        /// <summary>
        /// Evaluates the expression against the given resources.
        /// </summary>
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


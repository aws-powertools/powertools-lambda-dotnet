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
using System.Diagnostics;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Functions;
using AWS.Lambda.Powertools.JMESPath.Operators;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal enum TokenType
    {
        CurrentNode,
        LeftParen,
        RightParen,
        BeginMultiSelectHash,
        EndMultiSelectHash,
        BeginMultiSelectList,
        EndMultiSelectList,
        BeginFilter,
        EndFilter,
        Pipe,
        Separator,
        Key,
        Literal,
        Expression,
        BinaryOperator,
        UnaryOperator,
        Function,
        BeginArguments,
        EndArguments,
        Argument,
        BeginExpressionType,
        EndExpressionType,
        EndOfExpression
    }

    /// <summary>
    /// Represents a token in the JMESPath expression.
    /// </summary>
    internal readonly struct Token : IEquatable<Token>
    {
        /// <summary>
        /// The expression associated with this token.
        /// </summary>
        private readonly object? _expr;

        internal Token(TokenType type)
        {
            Type = type;
            _expr = null;
        }

        internal Token(TokenType type, string s)
        {
            Type = type;
            _expr = s;
        }

        internal Token(IExpression expr)
        {
            Type = TokenType.Expression;
            _expr = expr;
        }

        internal Token(IUnaryOperator expr)
        {
            Type = TokenType.UnaryOperator;
            _expr = expr;
        }

        internal Token(IBinaryOperator expr)
        {
            Type = TokenType.BinaryOperator;
            _expr = expr;
        }

        internal Token(IFunction expr)
        {
            Type = TokenType.Function;
            _expr = expr;
        }

        internal Token(IValue expr)
        {
            Type = TokenType.Literal;
            _expr = expr;
        }

        internal TokenType Type{get;}

        /// <summary>
        /// Return if it is an Operator
        /// </summary>
        internal bool IsOperator
        {
            get
            {
                switch(Type)
                {
                    case TokenType.UnaryOperator:
                        return true;
                    case TokenType.BinaryOperator:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// True if the expression is a projection, false otherwise.
        /// </summary>
        internal bool IsProjection
        {
            get
            {
                switch(Type)
                {
                    case TokenType.Expression:
                        return GetExpression().IsProjection;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// True if the expression is right-associative, false otherwise.
        /// </summary>
        internal bool IsRightAssociative
        {
            get
            {
                switch(Type)
                {
                    case TokenType.Expression:
                        return GetExpression().IsRightAssociative;
                    case TokenType.UnaryOperator:
                        return GetUnaryOperator().IsRightAssociative;
                    case TokenType.BinaryOperator:
                        return GetBinaryOperator().IsRightAssociative;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// The precedence level of the operator.
        /// </summary>
        internal int PrecedenceLevel 
        {
            get
            {
                switch(Type)
                {
                    case TokenType.Expression:
                        return GetExpression().PrecedenceLevel;
                    case TokenType.UnaryOperator:
                        return GetUnaryOperator().PrecedenceLevel;
                    case TokenType.BinaryOperator:
                        return GetBinaryOperator().PrecedenceLevel;
                    default:
                        return 100;
                }
            }
        }

        /// <summary>
        /// Returns the token expression key if Type is Key
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal string GetKey()
        {
            Debug.Assert(Type == TokenType.Key);
            return _expr as string ?? throw new InvalidOperationException("Key cannot be null");
        }

        /// <summary>
        /// Returns the token expression key if Type is UnaryOperator
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal IUnaryOperator GetUnaryOperator()
        {
            Debug.Assert(Type == TokenType.UnaryOperator);
            return _expr as IUnaryOperator ?? throw new InvalidOperationException("Unary operator cannot be null");
        }

        /// <summary>
        /// Returns the token expression key if Type is BinaryOperator
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal IBinaryOperator GetBinaryOperator()
        {
            Debug.Assert(Type == TokenType.BinaryOperator);
            return _expr as IBinaryOperator ?? throw new InvalidOperationException("Binary operator cannot be null");
        }

        /// <summary>
        /// Returns the token expression key if Type is Literal
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal IValue GetValue()
        {
            Debug.Assert(Type == TokenType.Literal);
            return _expr as IValue ?? throw new InvalidOperationException("Value cannot be null");
        }

        /// <summary>
        /// Returns the token expression key if Type is Function
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal IFunction GetFunction()
        {
            Debug.Assert(Type == TokenType.Function);
            return _expr as IFunction ?? throw new InvalidOperationException("Function cannot be null");
        }

        /// <summary>
        /// Returns the token expression key if Type is Expression
        /// </summary>
        internal IExpression GetExpression()
        {
            Debug.Assert(Type == TokenType.Expression);
            return _expr as IExpression ?? throw new InvalidOperationException("Expression cannot be null");
        }
        public bool Equals(Token other)
        {
            return Type == other.Type;
        }

        public override string ToString()
        {
            switch(Type)
            {
                case TokenType.BeginArguments:
                    return "BeginArguments";
                case TokenType.CurrentNode:
                    return "CurrentNode";
                case TokenType.LeftParen:
                    return "LeftParen";
                case TokenType.RightParen:
                    return "RightParen";
                case TokenType.BeginMultiSelectHash:
                    return "BeginMultiSelectHash";
                case TokenType.EndMultiSelectHash:
                    return "EndMultiSelectHash";
                case TokenType.BeginMultiSelectList:
                    return "BeginMultiSelectList";
                case TokenType.EndMultiSelectList:
                    return "EndMultiSelectList";
                case TokenType.BeginFilter:
                    return "BeginFilter";
                case TokenType.EndFilter:
                    return "EndFilter";
                case TokenType.Pipe:
                    return $"Pipe";
                case TokenType.Separator:
                    return "Separator";
                case TokenType.Key:
                    return $"Key {_expr}";
                case TokenType.Literal:
                    return $"Literal {_expr}";
                case TokenType.Expression:
                    return "Expression";
                case TokenType.BinaryOperator:
                    return $"BinaryOperator {_expr}";
                case TokenType.UnaryOperator:
                    return $"UnaryOperator {_expr}";
                case TokenType.Function:
                    return $"Function {_expr}";
                case TokenType.EndArguments:
                    return "EndArguments";
                case TokenType.Argument:
                    return "Argument";
                case TokenType.BeginExpressionType:
                    return "BeginExpressionType";
                case TokenType.EndExpressionType:
                    return "EndExpressionType";
                case TokenType.EndOfExpression:
                    return "EndOfExpression";
                default:
                    return "Other";
            }
        }
    }
}

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
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AWS.Lambda.Powertools.JMESPath
{
    /// <summary>
    /// Defines a custom exception object that is thrown when JMESPath parsing fails.
    /// </summary>    

    public sealed class JmesPathParseException : Exception
    {
        /// <summary>
        /// The line in the JMESPath string where a parse error was detected.
        /// </summary>
        private int LineNumber {get;}

        /// <summary>
        /// The column in the JMESPath string where a parse error was detected.
        /// </summary>
        private int ColumnNumber {get;}

        internal JmesPathParseException(string message, int line, int column)
            : base(message)
        {
            LineNumber = line;
            ColumnNumber = column;
        }

        /// <summary>
        /// Returns an error message that describes the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString ()
        {
            return $"{Message} at line {LineNumber} and column {ColumnNumber}";
        }
    }

    internal enum JmesPathState
    {
        Start,
        LhsExpression,
        RhsExpression,
        SubExpression,
        ExpressionType,
        ComparatorExpression,
        FunctionExpression,
        Argument,
        ExpressionOrExpressionType,
        QuotedString,
        RawString,
        RawStringEscapeChar,
        QuotedStringEscapeChar,
        EscapeU1, 
        EscapeU2, 
        EscapeU3, 
        EscapeU4, 
        EscapeExpectSurrogatePair1, 
        EscapeExpectSurrogatePair2, 
        EscapeU5, 
        EscapeU6, 
        EscapeU7, 
        EscapeU8, 
        Literal,
        KeyExpr,
        ValExpr,
        IdentifierOrFunctionExpr,
        UnquotedString,
        KeyValExpr,
        Number,
        Digit,
        IndexOrSliceExpression,
        BracketSpecifier,
        BracketSpecifierOrMultiSelectList,
        Filter,
        MultiSelectList,
        MultiSelectHash,
        RhsSliceExpressionStop,
        RhsSliceExpressionStep,
        ExpectRightBracket,
        ExpectRightParen,
        ExpectDot,
        ExpectRightBrace,
        ExpectColon,
        ExpectMultiSelectList,
        CmpLtOrLte,
        CmpEq,
        CmpGtOrGte,
        CmpNe,
        ExpectPipeOrOr,
        ExpectAnd
    }

    internal ref struct JmesPathParser
    {
        private readonly ReadOnlySpan<char> _span;
        private int _index;
        private int _column;
        private int _line;
        private readonly Stack<JmesPathState> _stateStack;
        private readonly Stack<Token>_outputStack;
        private readonly Stack<Token>_operatorStack;

        internal JmesPathParser(string input)
        {
            _span = input.AsSpan();
            _index = 0;
            _column = 1;
            _line = 1;
            _stateStack = new Stack<JmesPathState>();
            _outputStack = new Stack<Token>();
            _operatorStack = new Stack<Token>();
        }

        internal JsonTransformer Parse()
        {
            _stateStack.Clear();
            _outputStack.Clear();
            _operatorStack.Clear();
            _index = 0;
            _line = 1;
            _column = 1;

            var buffer = new StringBuilder();
            int? sliceStart = null;
            int? sliceStop = null;
            var sliceStep = 1;
            uint cp = 0;
            uint cp2 = 0;

            PushToken(new Token(TokenType.CurrentNode));
            _stateStack.Push(JmesPathState.Start);

            var syntaxErrorMsg = "Syntax error";
            while (_index < _span.Length)
            {
                var expectedRightBracket = "Expected right bracket";
                switch (_stateStack.Peek())
                {
                    case JmesPathState.Start: 
                    {
                        _stateStack.Pop();
                        _stateStack.Push(JmesPathState.RhsExpression);
                        _stateStack.Push(JmesPathState.LhsExpression);
                        break;
                    }
                    case JmesPathState.RhsExpression:
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.': 
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.SubExpression);
                                break;
                            case '|':
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.ExpectPipeOrOr);
                                break;
                            case '&':
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.ExpectAnd);
                                break;
                            case '<':
                            case '>':
                            case '=':
                            {
                                _stateStack.Push(JmesPathState.ComparatorExpression);
                                break;
                            }
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.CmpNe);
                                break;
                            }
                            case ')':
                            {
                                _stateStack.Pop();
                                break;
                            }
                            case '[':
                                _stateStack.Push(JmesPathState.BracketSpecifier);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    throw new JmesPathParseException(syntaxErrorMsg, _line, _column);
                                }
                                break;
                        }
                        break;
                    case JmesPathState.ComparatorExpression:
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '<':
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.CmpLtOrLte);
                                break;
                            case '>':
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.CmpGtOrGte);
                                break;
                            case '=':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.CmpEq);
                                break;
                            }
                            default:
                                if (_stateStack.Count > 1)
                                {
                                    _stateStack.Pop();
                                }
                                else
                                {
                                    throw new JmesPathParseException(syntaxErrorMsg, _line, _column);
                                }
                                break;
                        }
                        break;
                    case JmesPathState.LhsExpression: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '\"':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ValExpr);
                                _stateStack.Push(JmesPathState.QuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RawString);
                                ++_index;
                                ++_column;
                                break;
                            case '`':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Literal);
                                ++_index;
                                ++_column;
                                break;
                            case '{':
                                PushToken(new Token(TokenType.BeginMultiSelectHash));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.MultiSelectHash);
                                ++_index;
                                ++_column;
                                break;
                            case '*': // wildcard
                                PushToken(new Token(new ObjectProjection()));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '(':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(TokenType.LeftParen));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectRightParen);
                                _stateStack.Push(JmesPathState.RhsExpression);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                            }
                            case '!':
                            {
                                ++_index;
                                ++_column;
                                PushToken(new Token(NotOperator.Instance));
                                break;
                            }
                            case '@':
                                ++_index;
                                ++_column;
                                PushToken(new Token(new CurrentNode()));
                                _stateStack.Pop();
                                break;
                            case '[': 
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.BracketSpecifierOrMultiSelectList);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if ((_span[_index] >= 'A' && _span[_index] <= 'Z') || (_span[_index] >= 'a' && _span[_index] <= 'z') || (_span[_index] == '_'))
                                {
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.IdentifierOrFunctionExpr);
                                    _stateStack.Push(JmesPathState.UnquotedString);
                                    buffer.Append(_span[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    throw new JmesPathParseException("Expected identifier", _line, _column);
                                }
                                break;
                        }
                        break;
                    }

                    case JmesPathState.SubExpression: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '\"':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ValExpr);
                                _stateStack.Push(JmesPathState.QuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '{':
                                PushToken(new Token(TokenType.BeginMultiSelectHash));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.MultiSelectHash);
                                ++_index;
                                ++_column;
                                break;
                            case '*':
                                PushToken(new Token(new ObjectProjection()));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            case '[': 
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectMultiSelectList);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if ((_span[_index] >= 'A' && _span[_index] <= 'Z') || (_span[_index] >= 'a' && _span[_index] <= 'z') || (_span[_index] == '_'))
                                {
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.IdentifierOrFunctionExpr);
                                    _stateStack.Push(JmesPathState.UnquotedString);
                                    buffer.Append(_span[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    throw new JmesPathParseException("Expected identifier", _line, _column);
                                }
                                break;
                        }
                        break;
                    }
                    case JmesPathState.KeyExpr:
                        PushToken(new Token(TokenType.Key, buffer.ToString()));
                        buffer.Clear(); 
                        _stateStack.Pop(); 
                        break;
                    case JmesPathState.ValExpr:
                        PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        buffer.Clear();
                        _stateStack.Pop(); 
                        break;
                    case JmesPathState.ExpressionOrExpressionType:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '&':
                                PushToken(new Token(TokenType.BeginExpressionType));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpressionType);
                                _stateStack.Push(JmesPathState.RhsExpression);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Argument);
                                _stateStack.Push(JmesPathState.RhsExpression);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                        }
                        break;

                    case JmesPathState.IdentifierOrFunctionExpr:
                        switch(_span[_index])
                        {
                            case '(':
                            {
                                var functionName = buffer.ToString();
                                if (!BuiltInFunctions.Instance.TryGetFunction(functionName, out var func))
                                {
                                    throw new JmesPathParseException($"Function '{functionName}' not found", _line, _column);
                                }
                                buffer.Clear();
                                PushToken(new Token(func));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.FunctionExpression);
                                _stateStack.Push(JmesPathState.ExpressionOrExpressionType);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                            {
                                PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                                buffer.Clear();
                                _stateStack.Pop(); 
                                break;
                            }
                        }
                        break;

                    case JmesPathState.FunctionExpression:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Push(JmesPathState.ExpressionOrExpressionType);
                                ++_index;
                                ++_column;
                                break;
                            case ')':
                            {
                                PushToken(new Token(TokenType.EndArguments));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            }
                        }
                        break;

                    case JmesPathState.Argument:
                        PushToken(new Token(TokenType.Argument));
                        _stateStack.Pop();
                        break;

                    case JmesPathState.ExpressionType:
                        PushToken(new Token(TokenType.EndExpressionType));
                        PushToken(new Token(TokenType.Argument));
                        _stateStack.Pop();
                        break;

                    case JmesPathState.QuotedString: 
                        switch (_span[_index])
                        {
                            case '\"':
                                _stateStack.Pop(); // quotedString
                                ++_index;
                                ++_column;
                                break;
                            case '\\':
                                _stateStack.Push(JmesPathState.QuotedStringEscapeChar);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;

                    case JmesPathState.UnquotedString: 
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                _stateStack.Pop(); // unquotedString
                                SkipWhiteSpace();
                                break;
                            default:
                                if ((_span[_index] >= '0' && _span[_index] <= '9') || (_span[_index] >= 'A' && _span[_index] <= 'Z') || (_span[_index] >= 'a' && _span[_index] <= 'z') || (_span[_index] == '_'))
                                {
                                    buffer.Append(_span[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    _stateStack.Pop(); // unquotedString
                                }
                                break;
                        }
                        break;

                    case JmesPathState.RawStringEscapeChar:
                        switch (_span[_index])
                        {
                            case '\'':
                                buffer.Append(_span[_index]);
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append('\\');
                                buffer.Append(_span[_index]);
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;

                    case JmesPathState.QuotedStringEscapeChar:
                        switch (_span[_index])
                        {
                            case '\"':
                                buffer.Append('\"');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case '\\': 
                                buffer.Append('\\');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case '/':
                                buffer.Append('/');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'b':
                                buffer.Append('\b');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'f':
                                buffer.Append('\f');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'n':
                                buffer.Append('\n');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'r':
                                buffer.Append('\r');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 't':
                                buffer.Append('\t');
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                break;
                            case 'u':
                                ++_index;
                                ++_column;
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.EscapeU1);
                                break;
                            default:
                                throw new JmesPathParseException("Illegal escaped character", _line, _column);
                        }
                        break;

                    case JmesPathState.EscapeU1:
                        cp = AppendToCodepoint(0, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JmesPathState.EscapeU2);
                        break;
                    case JmesPathState.EscapeU2:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JmesPathState.EscapeU3);
                        break;
                    case JmesPathState.EscapeU3:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JmesPathState.EscapeU4);
                        break;
                    case JmesPathState.EscapeU4:
                        cp = AppendToCodepoint(cp, _span[_index]);
                        if (char.IsHighSurrogate((char)cp))
                        {
                            ++_index;
                            ++_column;
                            _stateStack.Pop(); 
                            _stateStack.Push(JmesPathState.EscapeExpectSurrogatePair1);
                        }
                        else
                        {
                            buffer.Append(char.ConvertFromUtf32((int)cp));
                            ++_index;
                            ++_column;
                            _stateStack.Pop();
                        }
                        break;
                    case JmesPathState.EscapeExpectSurrogatePair1:
                        switch (_span[_index])
                        {
                            case '\\': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(JmesPathState.EscapeExpectSurrogatePair2);
                                break;
                            default:
                                throw new JmesPathParseException("Invalid codepoint", _line, _column);
                        }
                        break;
                    case JmesPathState.EscapeExpectSurrogatePair2:
                        switch (_span[_index])
                        {
                            case 'u': 
                                ++_index;
                                ++_column;
                                _stateStack.Pop(); 
                                _stateStack.Push(JmesPathState.EscapeU5);
                                break;
                            default:
                                throw new JmesPathParseException("Invalid codepoint", _line, _column);
                        }
                        break;
                    case JmesPathState.EscapeU5:
                        cp2 = AppendToCodepoint(0, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JmesPathState.EscapeU6);
                        break;
                    case JmesPathState.EscapeU6:
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JmesPathState.EscapeU7);
                        break;
                    case JmesPathState.EscapeU7:
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        ++_index;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JmesPathState.EscapeU8);
                        break;
                    case JmesPathState.EscapeU8:
                    {
                        cp2 = AppendToCodepoint(cp2, _span[_index]);
                        var codepoint = 0x10000 + ((cp & 0x3FF) << 10) + (cp2 & 0x3FF);
                        buffer.Append(char.ConvertFromUtf32((int)codepoint));
                        _stateStack.Pop();
                        ++_index;
                        ++_column;
                        break;
                    }

                    case JmesPathState.RawString: 
                        switch (_span[_index])
                        {
                            case '\'':
                            {
                                PushToken(new Token(new StringValue(buffer.ToString())));
                                buffer.Clear();
                                _stateStack.Pop(); // rawString
                                ++_index;
                                ++_column;
                                break;
                            }
                            case '\\':
                                _stateStack.Push(JmesPathState.RawStringEscapeChar);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;

                    case JmesPathState.Literal: 
                        switch (_span[_index])
                        {
                            case '`':
                            {
                                try
                                {
                                    using (var doc = JsonDocument.Parse(buffer.ToString()))
                                    {            
                                        PushToken(new Token(new JsonElementValue(doc.RootElement.Clone())));
                                        buffer.Clear();
                                        _stateStack.Pop();
                                        ++_index;
                                    }
                                }
                                catch (JsonException)
                                {
                                    throw new JmesPathParseException("Invalid JSON literal", _line, _column);
                                }
                                break;
                            }
                            case '\\':
                                if (_index + 1 < _span.Length)
                                {
                                    ++_index;
                                    ++_column;
                                    if (_span[_index] != '`')
                                    {
                                        buffer.Append('\\');
                                    }
                                    buffer.Append(_span[_index]);
                                }
                                else
                                {
                                    throw new JmesPathParseException("Unexpected end of input", _line, _column);
                                }
                                ++_index;
                                ++_column;
                                break;
                            default:
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                        }
                        break;

                    case JmesPathState.Number:
                        switch(_span[_index])
                        {
                            case '-':
                                buffer.Append(_span[_index]);
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Digit);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Digit);
                                break;
                        }
                        break;
                    case JmesPathState.Digit:
                        switch(_span[_index])
                        {
                            case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                buffer.Append(_span[_index]);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                _stateStack.Pop(); // digit
                                break;
                        }
                        break;

                    case JmesPathState.BracketSpecifier:
                        switch(_span[_index])
                        {
                            case '*':
                                PushToken(new Token(new ListProjection()));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectRightBracket);
                                ++_index;
                                ++_column;
                                break;
                            case ']': // []
                                PushToken(new Token(new FlattenProjection()));
                                _stateStack.Pop(); // bracketSpecifier
                                ++_index;
                                ++_column;
                                break;
                            case '?':
                                PushToken(new Token(TokenType.BeginFilter));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.Filter);
                                _stateStack.Push(JmesPathState.RhsExpression);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                ++_index;
                                ++_column;
                                break;
                            case ':': // sliceExpression
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RhsSliceExpressionStop);
                                _stateStack.Push(JmesPathState.Number);
                                ++_index;
                                ++_column;
                                break;
                            // number
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.IndexOrSliceExpression);
                                _stateStack.Push(JmesPathState.Number);
                                break;
                            default:
                                throw new JmesPathParseException("Expected index expression", _line, _column);
                        }
                        break;
                    case JmesPathState.BracketSpecifierOrMultiSelectList:
                        switch(_span[_index])
                        {
                            case '*':
                                if (_index+1 >= _span.Length)
                                {
                                    throw new JmesPathParseException("Unexpected end of input", _line, _column);
                                }
                                if (_span[_index+1] == ']')
                                {
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.BracketSpecifier);
                                }
                                else
                                {
                                    PushToken(new Token(TokenType.BeginMultiSelectList));
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.MultiSelectList);
                                    _stateStack.Push(JmesPathState.LhsExpression);                                
                                }
                                break;
                            case ']': // []
                            case '?':
                            case ':': // sliceExpression
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.BracketSpecifier);
                                break;
                            default:
                                PushToken(new Token(TokenType.BeginMultiSelectList));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.MultiSelectList);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                        }
                        break;

                    case JmesPathState.ExpectMultiSelectList:
                        switch(_span[_index])
                        {
                            case ']':
                            case '?':
                            case ':':
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                throw new JmesPathParseException("Expected MultiSelectList", _line, _column);
                            case '*':
                                PushToken(new Token(new ListProjection()));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectRightBracket);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(TokenType.BeginMultiSelectList));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.MultiSelectList);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                        }
                        break;

                    case JmesPathState.MultiSelectHash:
                        switch(_span[_index])
                        {
                            case '*':
                            case ']':
                            case '?':
                            case ':':
                            case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                                break;
                            default:
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.KeyValExpr);
                                break;
                        }
                        break;

                    case JmesPathState.IndexOrSliceExpression:
                        switch(_span[_index])
                        {
                            case ']':
                            {
                                if (buffer.Length == 0)
                                {
                                    PushToken(new Token(new FlattenProjection()));
                                }
                                else
                                {
                                    if (!int.TryParse(buffer.ToString(), out var n))
                                    {
                                        throw new JmesPathParseException("Invalid number", _line, _column);
                                    }
                                    PushToken(new Token(new IndexSelector(n)));
                                    buffer.Clear();
                                }
                                _stateStack.Pop(); // bracketSpecifier
                                ++_index;
                                ++_column;
                                break;
                            }
                            case ':':
                            {
                                var s = buffer.ToString();
                                if (!int.TryParse(s, out var n))
                                {
                                    n = s.StartsWith('-') ? int.MinValue : int.MaxValue;
                                }
                                sliceStart = n;
                                buffer.Clear();
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RhsSliceExpressionStop);
                                _stateStack.Push(JmesPathState.Number);
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JmesPathParseException(expectedRightBracket, _line, _column);
                        }
                        break;
                    case JmesPathState.RhsSliceExpressionStop :
                    {
                        if (buffer.Length != 0)
                        {
                            var s = buffer.ToString();
                            if (!int.TryParse(s, out var n))
                            {
                                n = s.StartsWith('-') ? int.MinValue : int.MaxValue;
                            }
                            sliceStop = n;
                            buffer.Clear();
                        }
                        switch(_span[_index])
                        {
                            case ']':
                                PushToken(new Token(new SliceProjection(new Slice(sliceStart,sliceStop,sliceStep))));
                                sliceStart = null;
                                sliceStop = null;
                                sliceStep = 1;
                                _stateStack.Pop(); // bracketSpecifier2
                                ++_index;
                                ++_column;
                                break;
                            case ':':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RhsSliceExpressionStep);
                                _stateStack.Push(JmesPathState.Number);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JmesPathParseException(expectedRightBracket, _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.RhsSliceExpressionStep:
                    {
                        if (buffer.Length != 0)
                        {
                            if (!int.TryParse(buffer.ToString(), out var n))
                            {
                                throw new JmesPathParseException("Invalid slice stop", _line, _column);
                            }
                            buffer.Clear();
                            if (n == 0)
                            {
                                throw new JmesPathParseException("Slice step cannot be zero", _line, _column);
                            }
                            sliceStep = n;
                            buffer.Clear();
                        }
                        switch(_span[_index])
                        {
                            case ']':
                                PushToken(new Token(new SliceProjection(new Slice(sliceStart,sliceStop,sliceStep))));
                                sliceStart = null;
                                sliceStop = null;
                                sliceStep = 1;
                                buffer.Clear();
                                _stateStack.Pop(); // rhsSliceExpressionStep
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JmesPathParseException(expectedRightBracket, _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.ExpectRightBracket:
                    {
                        switch(_span[_index])
                        {
                            case ']':
                                _stateStack.Pop(); // expectRightBracket
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JmesPathParseException(expectedRightBracket, _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.ExpectRightParen:
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ')':
                                ++_index;
                                ++_column;
                                PushToken(new Token(TokenType.RightParen));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.RhsExpression);
                                break;
                            default:
                                throw new JmesPathParseException("Expected right parenthesis", _line, _column);
                        }
                        break;
                    case JmesPathState.KeyValExpr: 
                    {
                        switch (_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '\"':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectColon);
                                _stateStack.Push(JmesPathState.KeyExpr);
                                _stateStack.Push(JmesPathState.QuotedString);
                                ++_index;
                                ++_column;
                                break;
                            case '\'':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectColon);
                                _stateStack.Push(JmesPathState.RawString);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                if ((_span[_index] >= 'A' && _span[_index] <= 'Z') || (_span[_index] >= 'a' && _span[_index] <= 'z') || (_span[_index] == '_'))
                                {
                                    _stateStack.Pop();
                                    _stateStack.Push(JmesPathState.ExpectColon);
                                    _stateStack.Push(JmesPathState.KeyExpr);
                                    _stateStack.Push(JmesPathState.UnquotedString);
                                    buffer.Append(_span[_index]);
                                    ++_index;
                                    ++_column;
                                }
                                else
                                {
                                    throw new JmesPathParseException("Expected key", _line, _column);
                                }
                                break;
                        }
                        break;
                    }
                    case JmesPathState.CmpLtOrLte:
                    {
                        switch(_span[_index])
                        {
                            case '=':
                                PushToken(new Token(LteOperator.Instance));
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(LtOperator.Instance));
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Pop();
                                break;
                        }
                        break;
                    }
                    case JmesPathState.CmpGtOrGte:
                    {
                        switch(_span[_index])
                        {
                            case '=':
                                PushToken(new Token(GteOperator.Instance));
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(GtOperator.Instance));
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Pop(); 
                                break;
                        }
                        break;
                    }
                    case JmesPathState.CmpEq:
                    {
                        switch(_span[_index])
                        {
                            case '=':
                                PushToken(new Token(EqOperator.Instance));
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JmesPathParseException("Expected comparator", _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.CmpNe:
                    {
                        switch(_span[_index])
                        {
                            case '=':
                                PushToken(new Token(NeOperator.Instance));
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JmesPathParseException("Expected comparator", _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.ExpectDot:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case '.':
                                _stateStack.Pop(); // expect_dot
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JmesPathParseException("Expected dot", _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.ExpectPipeOrOr:
                    {
                        switch(_span[_index])
                        {
                            case '|':
                                PushToken(new Token(OrOperator.Instance));
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Pop(); 
                                ++_index;
                                ++_column;
                                break;
                            default:
                                PushToken(new Token(TokenType.Pipe));
                                _stateStack.Pop(); 
                                break;
                        }
                        break;
                    }
                    case JmesPathState.ExpectAnd:
                    {
                        switch(_span[_index])
                        {
                            case '&':
                                PushToken(new Token(AndOperator.Instance));
                                PushToken(new Token(TokenType.CurrentNode));
                                _stateStack.Pop(); // expectAnd
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JmesPathParseException("Expected &&", _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.MultiSelectList:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                                PushToken(new Token(TokenType.Separator));
                                _stateStack.Push(JmesPathState.LhsExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                            case '.':
                                _stateStack.Push(JmesPathState.SubExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '|':
                            {
                                ++_index;
                                ++_column;
                                _stateStack.Push(JmesPathState.LhsExpression);
                                _stateStack.Push(JmesPathState.ExpectPipeOrOr);
                                break;
                            }
                            case ']':
                            {
                                PushToken(new Token(TokenType.EndMultiSelectList));
                                _stateStack.Pop();

                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JmesPathParseException(expectedRightBracket, _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.Filter:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ']':
                            {
                                PushToken(new Token(TokenType.EndFilter));
                                _stateStack.Pop();
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JmesPathParseException(expectedRightBracket, _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.ExpectRightBrace:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ',':
                                PushToken(new Token(TokenType.Separator));
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.KeyValExpr); 
                                ++_index;
                                ++_column;
                                break;
                            case '[':
                            case '{':
                                _stateStack.Push(JmesPathState.LhsExpression);
                                break;
                            case '.':
                                _stateStack.Push(JmesPathState.SubExpression);
                                ++_index;
                                ++_column;
                                break;
                            case '}':
                            {
                                _stateStack.Pop();
                                PushToken(new Token(TokenType.EndMultiSelectHash));
                                ++_index;
                                ++_column;
                                break;
                            }
                            default:
                                throw new JmesPathParseException("Expected right brace", _line, _column);
                        }
                        break;
                    }
                    case JmesPathState.ExpectColon:
                    {
                        switch(_span[_index])
                        {
                            case ' ':case '\t':case '\r':case '\n':
                                SkipWhiteSpace();
                                break;
                            case ':':
                                _stateStack.Pop();
                                _stateStack.Push(JmesPathState.ExpectRightBrace);
                                _stateStack.Push(JmesPathState.LhsExpression);
                                ++_index;
                                ++_column;
                                break;
                            default:
                                throw new JmesPathParseException("Expected colon", _line, _column);
                        }
                        break;
                    }
                }
            }

            if (_stateStack.Count == 0)
            {
                throw new JmesPathParseException(syntaxErrorMsg, _line, _column);
            }
            while (_stateStack.Count > 1)
            {
                switch (_stateStack.Peek())
                {
                    case JmesPathState.RhsExpression:
                        if (_stateStack.Count > 1)
                        {
                            _stateStack.Pop();
                        }
                        else
                        {
                            throw new JmesPathParseException(syntaxErrorMsg, _line, _column);
                        }
                        break;
                    case JmesPathState.ValExpr:
                    case JmesPathState.IdentifierOrFunctionExpr:
                        PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                        _stateStack.Pop(); 
                        break;
                    case JmesPathState.UnquotedString: 
                        _stateStack.Pop(); 
                        break;
                    default:
                        throw new JmesPathParseException(syntaxErrorMsg, _line, _column);
                }
            }

            if (!(_stateStack.Count == 1 && _stateStack.Peek() == JmesPathState.RhsExpression))
            {
                throw new JmesPathParseException("Unexpected end of input", _line, _column);
            }

            _stateStack.Pop();

            PushToken(new Token(TokenType.EndOfExpression));

            var a = _outputStack.ToArray();

            return new JsonTransformer(new Expression(a));
        }

        private void SkipWhiteSpace()
        {
            switch (_span[_index])
            {
                case ' ':case '\t':
                    ++_index;
                    ++_column;
                    break;
                case '\r':
                    if (_index+1 < _span.Length && _span[_index+1] == '\n')
                        ++_index;
                    ++_line;
                    _column = 1;
                    ++_index;
                    break;
                case '\n':
                    ++_line;
                    _column = 1;
                    ++_index;
                    break;
            }
        }

        private void UnwindRightParen()
        {
            while (_operatorStack.Count > 1 && _operatorStack.Peek().Type != TokenType.LeftParen)
            {
                _outputStack.Push(_operatorStack.Pop());
            }
            if (_operatorStack.Count == 0)
            {
                throw new JmesPathParseException("Unbalanced parentheses", _line, _column);
            }
            _operatorStack.Pop(); // TokenType.LeftParen
        }

        private void PushToken(Token token)
        {
            switch (token.Type)
            {
                case TokenType.EndFilter:
                {
                    UnwindRightParen();
                    var tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenType.BeginFilter)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JmesPathParseException("Unbalanced parentheses", _line, _column);
                    }
                    if (tokens[tokens.Count-1].Type != TokenType.Literal)
                    {
                        tokens.Add(new Token(TokenType.CurrentNode));
                    }
                    _outputStack.Pop();

                    if (_outputStack.Count != 0 && _outputStack.Peek().IsProjection && 
                        (token.PrecedenceLevel > _outputStack.Peek().PrecedenceLevel ||
                        (token.PrecedenceLevel == _outputStack.Peek().PrecedenceLevel && token.IsRightAssociative)))
                    {
                        _outputStack.Peek().GetExpression().AddExpression(new FilterExpression(new Expression(tokens.ToArray())));
                    }
                    else
                    {
                        _outputStack.Push(new Token(new FilterExpression(new Expression(tokens.ToArray()))));
                    }
                    break;
                }
                case TokenType.EndMultiSelectList:
                {
                    UnwindRightParen();
                    var expressions = new List<Expression>();
                    while (_outputStack.Count > 0 && _outputStack.Peek().Type != TokenType.BeginMultiSelectList)
                    {
                        var tokens = new List<Token>();
                        do
                        {
                            tokens.Add(_outputStack.Pop());
                        }
                        while (_outputStack.Count > 0 && _outputStack.Peek().Type != TokenType.BeginMultiSelectList && _outputStack.Peek().Type != TokenType.Separator);
                        if (_outputStack.Peek().Type == TokenType.Separator)
                        {
                            _outputStack.Pop();
                        }
                        if (tokens[tokens.Count-1].Type != TokenType.Literal)
                        {
                            tokens.Add(new Token(TokenType.CurrentNode));
                        }
                        expressions.Add(new Expression(tokens.ToArray()));
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JmesPathParseException("Unbalanced braces", _line, _column);
                    }
                    _outputStack.Pop(); // TokenType.BeginMultiSelectList
                    expressions.Reverse();

                    if (_outputStack.Count != 0 && _outputStack.Peek().IsProjection && 
                        (token.PrecedenceLevel > _outputStack.Peek().PrecedenceLevel ||
                        (token.PrecedenceLevel == _outputStack.Peek().PrecedenceLevel && token.IsRightAssociative)))
                    {
                        _outputStack.Peek().GetExpression().AddExpression(new MultiSelectList(expressions));
                    }
                    else
                    {
                        _outputStack.Push(new Token(new MultiSelectList(expressions)));
                    }
                    break;
                }
                
                case TokenType.EndMultiSelectHash:
                {
                    UnwindRightParen();
                    var keyExprPairs = new List<KeyExpressionPair>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenType.BeginMultiSelectHash)
                    {
                        var tokens = new List<Token>();
                        do
                        {
                            tokens.Add(_outputStack.Pop());
                        }
                        while (_outputStack.Peek().Type != TokenType.Key);
                        if (_outputStack.Peek().Type != TokenType.Key)
                        {
                            throw new JmesPathParseException("Syntax error", _line, _column);
                        }
                        var key = _outputStack.Pop().GetKey();
                        if (_outputStack.Peek().Type == TokenType.Separator)
                        {
                            _outputStack.Pop();
                        }
                        if (tokens[tokens.Count-1].Type != TokenType.Literal)
                        {
                            tokens.Add(new Token(TokenType.CurrentNode));
                        }
                        keyExprPairs.Add(new KeyExpressionPair(key, new Expression(tokens.ToArray())));
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JmesPathParseException("Syntax error", _line, _column);
                    }
                    keyExprPairs.Reverse();
                    _outputStack.Pop(); // TokenType.BeginMultiSelectHash
                 
                    if (_outputStack.Count != 0 && _outputStack.Peek().IsProjection && 
                        (token.PrecedenceLevel > _outputStack.Peek().PrecedenceLevel ||
                        (token.PrecedenceLevel == _outputStack.Peek().PrecedenceLevel && token.IsRightAssociative)))
                    {
                        _outputStack.Peek().GetExpression().AddExpression(new MultiSelectHash(keyExprPairs));
                    }
                    else
                    {
                        _outputStack.Push(new Token(new MultiSelectHash(keyExprPairs)));
                    }
                    break;
                }
                case TokenType.EndExpressionType:
                {
                    var tokens = new List<Token>();
                    while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenType.BeginExpressionType)
                    {
                        tokens.Add(_outputStack.Pop());
                    }
                    if (_outputStack.Count == 0)
                    {
                        throw new JmesPathParseException("Unbalanced braces", _line, _column);
                    }
                    if (tokens[tokens.Count-1].Type != TokenType.Literal)
                    {
                        tokens.Add(new Token(TokenType.CurrentNode));
                    }
                    _outputStack.Push(new Token(new FunctionExpression(new Expression(tokens.ToArray()))));
                    break;
                }
                case TokenType.Literal:
                    if (_outputStack.Count != 0 && _outputStack.Peek().Type == TokenType.CurrentNode)
                    {
                        _outputStack.Pop();
                        _outputStack.Push(token);
                    }
                    else
                    {
                        _outputStack.Push(token);
                    }
                    break;
                case TokenType.Expression:
                    if (_outputStack.Count != 0 && _outputStack.Peek().IsProjection && 
                        (token.PrecedenceLevel > _outputStack.Peek().PrecedenceLevel ||
                        (token.PrecedenceLevel == _outputStack.Peek().PrecedenceLevel && token.IsRightAssociative)))
                    {
                        _outputStack.Peek().GetExpression().AddExpression(token.GetExpression());
                    }
                    else
                    {
                        _outputStack.Push(token);
                    }
                    break;
                case TokenType.RightParen:
                    {
                        UnwindRightParen();
                        break;
                    }
                case TokenType.EndArguments:
                    {
                        UnwindRightParen();
                        var argCount = 0;
                        var tokens = new List<Token>();
                        Debug.Assert(_operatorStack.Count > 0 && _operatorStack.Peek().Type == TokenType.Function);
                        tokens.Add(_operatorStack.Pop()); // Function
                        while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenType.BeginArguments)
                        {
                            if (_outputStack.Peek().Type == TokenType.Argument)
                            {
                                ++argCount;
                            }
                            tokens.Add(_outputStack.Pop());
                        }
                        if (_outputStack.Count == 0)
                        {
                            throw new JmesPathParseException("Expected parentheses", _line, _column);
                        }
                        _outputStack.Pop(); // TokenType.BeginArguments
                        if (tokens[tokens.Count-1].Type != TokenType.Literal)
                        {
                            tokens.Add(new Token(TokenType.CurrentNode));
                        }
                        if (tokens[0].GetFunction().Arity != null && argCount != tokens[0].GetFunction().Arity)
                        {
                            throw new JmesPathParseException($"Invalid arity (The number of arguments or operands a function or operation takes) calling function '{tokens[0].GetFunction()}', expected {tokens[0].GetFunction().Arity}, found {argCount}", _line, _column);
                        }

                        if (_outputStack.Count != 0 && _outputStack.Peek().IsProjection && 
                            (token.PrecedenceLevel > _outputStack.Peek().PrecedenceLevel ||
                            (token.PrecedenceLevel == _outputStack.Peek().PrecedenceLevel && token.IsRightAssociative)))
                        {
                            _outputStack.Peek().GetExpression().AddExpression(new FunctionExpression(new Expression(tokens.ToArray())));
                        }
                        else
                        {
                            _outputStack.Push(new Token(new FunctionExpression(new Expression(tokens.ToArray()))));
                        }
                        break;
                    }
                case TokenType.EndOfExpression:
                    {
                        while (_operatorStack.Count != 0)
                        {
                            _outputStack.Push(_operatorStack.Pop());
                        }
                        break;
                    }
                case TokenType.UnaryOperator:
                case TokenType.BinaryOperator:
                {
                    if (_operatorStack.Count == 0 || _operatorStack.Peek().Type == TokenType.LeftParen)
                    {
                        _operatorStack.Push(token);
                    }
                    else if (token.PrecedenceLevel > _operatorStack.Peek().PrecedenceLevel
                             || (token.PrecedenceLevel == _operatorStack.Peek().PrecedenceLevel && token.IsRightAssociative))
                    {
                        _operatorStack.Push(token);
                    }
                    else
                    {
                        while (_operatorStack.Count > 0 && _operatorStack.Peek().IsOperator
                               && (_operatorStack.Peek().PrecedenceLevel > token.PrecedenceLevel
                             || (token.PrecedenceLevel == _operatorStack.Peek().PrecedenceLevel && token.IsRightAssociative)))
                        {
                            _outputStack.Push(_operatorStack.Pop());
                        }

                        _operatorStack.Push(token);
                    }
                    break;
                }
                case TokenType.Separator:
                {
                    UnwindRightParen();
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(TokenType.LeftParen));
                    break;
                }
                case TokenType.BeginMultiSelectHash:
                case TokenType.BeginMultiSelectList:
                case TokenType.BeginFilter:
                    _outputStack.Push(token);
                    _operatorStack.Push(new Token(TokenType.LeftParen));
                    break;
                case TokenType.Function:
                    _outputStack.Push(new Token(TokenType.BeginArguments));
                    _operatorStack.Push(token);
                    _operatorStack.Push(new Token(TokenType.LeftParen));
                    break;
                case TokenType.CurrentNode:
                case TokenType.Key:
                case TokenType.Pipe:
                case TokenType.Argument:
                case TokenType.BeginExpressionType:
                    _outputStack.Push(token);
                    break;
                case TokenType.LeftParen:
                    _operatorStack.Push(token);
                    break;
            }
        }
        
        private uint AppendToCodepoint(uint cp, uint c)
        {
            cp *= 16;
            switch (c)
            {
                case >= '0' and <= '9':
                    cp += c - '0';
                    break;
                case >= 'a' and <= 'f':
                    cp += c - 'a' + 10;
                    break;
                case >= 'A' and <= 'F':
                    cp += c - 'A' + 10;
                    break;
                default:
                    throw new JmesPathParseException("Invalid codepoint", _line, _column);
            }
            return cp;
        }
    }
}

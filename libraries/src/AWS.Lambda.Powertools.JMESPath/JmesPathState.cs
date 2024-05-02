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

namespace AWS.Lambda.Powertools.JMESPath;

/// <summary>
/// The state of the JMESPath parser.
/// </summary>
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
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

namespace AWS.Lambda.Powertools.JMESPath.Expressions;

/// <summary>
/// A pair of a JMESPath key and an expression.
/// </summary>
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
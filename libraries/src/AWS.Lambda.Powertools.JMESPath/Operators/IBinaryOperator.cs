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

using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Operators;

/// <summary>
/// Interface for all binary operators.
/// </summary>
internal interface IBinaryOperator 
{
    /// <summary>
    /// The precedence level of the operator.
    /// </summary>
    int PrecedenceLevel {get;}
    
    /// <summary>
    /// Whether the operator is right-associative.
    /// </summary>
    bool IsRightAssociative {get;}
    
    /// <summary>
    /// Evaluates the expression.
    /// </summary>
    bool TryEvaluate(IValue lhs, IValue rhs, out IValue result);
}
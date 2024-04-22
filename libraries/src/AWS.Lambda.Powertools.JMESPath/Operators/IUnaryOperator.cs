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

using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Operators;

/// <summary>
/// Interface for unary operators.
/// </summary>
internal interface IUnaryOperator 
{
    /// <summary>
    /// The precedence level of the operator.
    /// </summary>
    int PrecedenceLevel {get;}
    
    /// <summary>
    /// Whether the operator is right-associative or not.
    /// </summary>
    bool IsRightAssociative {get;}
    
    /// <summary>
    /// Evaluates the expression.
    /// </summary>
    /// <param name="elem"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    bool TryEvaluate(IValue elem, out IValue result);
}
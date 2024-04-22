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

namespace AWS.Lambda.Powertools.JMESPath.Expressions;

/// <summary>
/// Represents a JMESPath expression.
/// </summary>
internal interface IExpression
{
    /// <summary>
    /// Evaluates the expression against the provided resources.
    /// </summary>
    /// <param name="resources"></param>
    /// <param name="current"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    bool TryEvaluate(DynamicResources resources,
        IValue current, 
        out IValue value);

    /// <summary>
    /// The precedence level of the expression.
    /// </summary>
    int PrecedenceLevel {get;} 

    /// <summary>
    /// True if the expression is a projection, false otherwise.
    /// </summary>
    bool IsProjection {get;} 

    /// <summary>
    /// True if the expression is right-associative, false otherwise.
    /// </summary>
    bool IsRightAssociative {get;}

    /// <summary>
    /// Adds an expression to the expression.
    /// </summary>
    void AddExpression(IExpression expr);
}
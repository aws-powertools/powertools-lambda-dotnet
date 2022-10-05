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
using AspectInjector.Broker;
using AWS.Lambda.Powertools.Idempotency.Internal;

namespace AWS.Lambda.Powertools.Idempotency;

/// <summary>
/// Idempotent is used to signal that the annotated method is idempotent:
/// Calling this method one or multiple times with the same parameter will always return the same result.
/// This annotation can be placed on any method of a Lambda function
/// <code>
/// [Idempotent]
/// public Task&lt;string&gt; FunctionHandler(string input, ILambdaContext context)
/// {
///  return Task.FromResult(input.ToUpper());
/// }
/// </code>
///     Environment variables                                                                         <br/>
///     ---------------------                                                                         <br/> 
///     <list type="table">
///         <listheader>
///           <term>Variable name</term>
///           <description>Description</description>
///         </listheader>
///         <item>
///             <term>AWS_LAMBDA_FUNCTION_NAME</term>
///             <description>string, function name</description>
///         </item>
///         <item>
///             <term>AWS_REGION</term>
///             <description>string, AWS region</description>
///         </item>
///         <item>
///             <term>POWERTOOLS_IDEMPOTENCY_DISABLED</term>
///             <description>string, Enable or disable the Idempotency</description>
///         </item>
///     </list>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[Injection(typeof(IdempotentAspect), Inherited = true)]
public class IdempotentAttribute : Attribute
{
}
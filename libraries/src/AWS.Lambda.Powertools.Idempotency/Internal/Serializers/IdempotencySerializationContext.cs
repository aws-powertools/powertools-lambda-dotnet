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
using System.Text.Json.Serialization;

namespace AWS.Lambda.Powertools.Idempotency.Internal.Serializers;

/// <summary>
/// The source generated JsonSerializerContext to be used to Serialize Idempotency types
/// </summary>
/// <remarks>
/// Includes the value types that <c>[Idempotent]</c> methods can return under Native AOT, so their results can be
/// stored and replayed without the user registering them in their own context.
/// </remarks>
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
public partial class IdempotencySerializationContext : JsonSerializerContext
{
    
}
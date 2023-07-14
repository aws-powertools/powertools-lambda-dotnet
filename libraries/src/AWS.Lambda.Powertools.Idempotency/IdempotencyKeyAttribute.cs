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

namespace AWS.Lambda.Powertools.Idempotency;

/// <summary>
/// IdempotencyKey is used to signal that a method parameter is used as a key for idempotency.
/// Must be used in conjunction with the Idempotency attribute.
/// 
/// Example:
/// 
/// [Idempotent]
/// private Basket SubMethod([IdempotencyKey]string magicProduct, Product p) { ... }
/// Note: This annotation is not needed when the method only has one parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class IdempotencyKeyAttribute: Attribute
{
    
}
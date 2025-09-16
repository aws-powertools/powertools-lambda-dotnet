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
using System.Text.Json.Serialization;

namespace HelloWorld.Data;

public class Order
{
    public string? OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string? CustomerId { get; set; }
    public List<Product> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string? Status { get; set; }
}

public class Customer
{
    public string? CustomerId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// JsonSerializerContext for AOT compatibility
/// </summary>
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(List<Product>))]
[JsonSerializable(typeof(List<Order>))]
public partial class ExampleJsonSerializerContext : JsonSerializerContext
{
}
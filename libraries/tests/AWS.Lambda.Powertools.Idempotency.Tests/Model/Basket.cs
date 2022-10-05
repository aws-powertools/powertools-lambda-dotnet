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

using System.Collections.Generic;
using System.Linq;

namespace AWS.Lambda.Powertools.Idempotency.Tests.Model;

public class Basket
{
    public List<Product> Products { get; set; } = new List<Product>();
    public Basket()
    {
        
    }

    public Basket(params Product[] products)
    {
        Products.AddRange(products);
    }

    public void Add(Product product)
    {
        Products.Add(product);
    }

    protected bool Equals(Basket other)
    {
        return Products.All(other.Products.Contains);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Basket) obj);
    }

    public override int GetHashCode()
    {
        return Products.GetHashCode();
    }
}
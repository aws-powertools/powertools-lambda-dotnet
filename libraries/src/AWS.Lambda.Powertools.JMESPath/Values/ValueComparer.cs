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

using System;
using System.Collections.Generic;
using System.Linq;

namespace AWS.Lambda.Powertools.JMESPath.Values
{
    /// <summary>
    /// Compares two <see cref="IValue"/> instances.
    /// </summary>
    internal sealed class ValueComparer : IComparer<IValue>, System.Collections.IComparer
    {
        /// <summary>Gets a singleton instance of <see cref="ValueComparer"/>. This property is read-only.</summary>
        public static ValueComparer Instance { get; } = new();

        /// <summary>
        /// Constructs a <see cref="ValueComparer"/>
        /// </summary>
        private ValueComparer() {}

        /// <summary>
        /// Compares two <see cref="IValue"/> instances.
        /// 
        /// If the two <see cref="IValue"/> instances have different data types, they are
        /// compared according to their Type property, which gives this ordering:
        /// <code>
        ///    Undefined
        ///    Object
        ///    Array
        ///    String
        ///    Number
        ///    True
        ///    False
        ///    Null
        /// </code>
        /// 
        /// If both <see cref="IValue"/> instances are null, true, or false, they are equal.
        /// 
        /// If both are strings, they are compared with the String.CompareTo method.
        /// 
        /// If both are numbers, and both can be represented by a <see cref="Decimal"/>,
        /// they are compared with the Decimal.CompareTo method, otherwise they are
        /// compared as doubles.
        /// 
        /// If both are objects, they are compared accoring to the following rules:
        /// 
        /// <ul>
        /// <li>Order each object's properties by name and compare sequentially.
        /// The properties are compared first by name with the String.CompareTo method, then by value with <see cref="ValueComparer"/></li>
        /// <li> The first mismatching property defines which <see cref="IValue"/> instance is less or greater than the other.</li>
        /// <li> If the two sequences have no mismatching properties until one of them ends, and the other is longer, the shorter sequence is less than the other.</li>
        /// <li> If the two sequences have no mismatching properties and have the same length, they are equal.</li>
        /// </ul>  
        /// 
        /// If both are arrays, they are compared element wise with <see cref="ValueComparer"/>.
        /// The first mismatching element defines which <see cref="IValue"/> instance is less or greater than the other.
        /// If the two arrays have no mismatching elements until one of them ends, and the other is longer, the shorter array is less than the other.
        /// If the two arrays have no mismatching elements and have the same length, they are equal.
        /// 
        /// </summary>
        /// <param name="lhs">The first object of type cref="IValue"/> to compare.</param>
        /// <param name="rhs">The second object of type cref="IValue"/> to compare.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        ///   Unable to compare numbers as either <see cref="Decimal"/> or double (shouldn't happen.)
        /// </exception>
        public int Compare(IValue lhs, IValue rhs)
        {
            if (lhs.Type != rhs.Type)
                return (int)lhs.Type - (int)rhs.Type;
    
            switch (lhs.Type)
            {
                case JmesPathType.Null:
                case JmesPathType.True:
                case JmesPathType.False:
                    return 0;
    
                case JmesPathType.Number:
                {
                    if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
                    {
                        return dec1.CompareTo(dec2);
                    }

                    if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
                    {
                        return val1.CompareTo(val2);
                    }

                    throw new InvalidOperationException("Unable to compare numbers");
                }
    
                case JmesPathType.String:
                    return string.Compare(lhs.GetString(), rhs.GetString(), StringComparison.Ordinal); 
    
                case JmesPathType.Array:
                {
                    // 2024-04-19: Powertools addition.
                    return ArrayComparer(lhs, rhs);
                }

                case JmesPathType.Object:
                {
                    // 2024-04-19: Powertools addition.
                    return ObjectComparer(lhs, rhs);
                }
    
                default:
                    throw new InvalidOperationException($"Unknown JmesPathType {lhs.Type}");
            }
        }

        // 2024-04-19: Powertools addition.
        private int ArrayComparer(IValue lhs, IValue rhs)
        {
            var enumerator1 = lhs.EnumerateArray();
            var enumerator2 = rhs.EnumerateArray();
            var result1 = enumerator1.MoveNext();
            var result2 = enumerator2.MoveNext();
            while (result1 && result2)
            {
                var diff = Compare(enumerator1.Current, enumerator2.Current);
                if (diff != 0)
                {
                    return diff;
                }
                result1 = enumerator1.MoveNext();
                result2 = enumerator2.MoveNext();
            }   
            return result1 ? 1 : result2 ? -1 : 0;
        }

        // 2024-04-19: Powertools addition.
        private int ObjectComparer(IValue lhs, IValue rhs)
        {
            // OrderBy performs a stable sort (Note that <see cref="IValue"/> supports duplicate property names)
            using var enumerator1 = lhs.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).GetEnumerator();
            using var enumerator2 = rhs.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).GetEnumerator();
    
            var result1 = enumerator1.MoveNext();
            var result2 = enumerator2.MoveNext();
            while (result1 && result2)
            {
                if (enumerator1.Current.Name != enumerator2.Current.Name)
                {
                    return string.Compare(enumerator1.Current.Name, enumerator2.Current.Name, StringComparison.Ordinal);
                }
                var diff = Compare(enumerator1.Current.Value, enumerator2.Current.Value);
                if (diff != 0)
                {
                    return diff;
                }
                result1 = enumerator1.MoveNext();
                result2 = enumerator2.MoveNext();
            }   
    
            return result1 ? 1 : result2 ? -1 : 0;
        }

        int System.Collections.IComparer.Compare(object x, object y)
        {
            return Compare((IValue)x, (IValue)y);
        }        
    }
}

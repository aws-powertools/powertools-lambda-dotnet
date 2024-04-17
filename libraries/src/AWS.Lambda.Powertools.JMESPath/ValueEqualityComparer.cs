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
using System.Linq;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal sealed class ValueEqualityComparer : IEqualityComparer<IValue>
   {
       internal static ValueEqualityComparer Instance { get; } = new();

       private readonly int _maxHashDepth = 100;

       private ValueEqualityComparer() {}

       public bool Equals(IValue lhs, IValue rhs)
       {
           if (lhs != null && rhs != null && lhs.Type != rhs.Type)
               return false;

           if (rhs == null || lhs == null) return false;
           
           switch (lhs.Type)
           {
               case JmesPathType.Null:
               case JmesPathType.True:
               case JmesPathType.False:
                   return true;

               case JmesPathType.Number:
               {
                   if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
                   {
                       return dec1 == dec2;
                   }

                   if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
                   {
                       return Math.Abs(val1 - val2) < 0.000000001;
                   }

                   return false;
               }

               case JmesPathType.String:
                   return lhs.GetString().Equals(rhs.GetString());

               case JmesPathType.Array:
                   return lhs.EnumerateArray().SequenceEqual(rhs.EnumerateArray(), this);

               case JmesPathType.Object:
               {
                   // OrderBy performs a stable sort (Note that IValue supports duplicate property names)
                   using var enumerator1 = lhs.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)
                       .GetEnumerator();
                   using var enumerator2 = rhs.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)
                       .GetEnumerator();

                   var result1 = enumerator1.MoveNext();
                   var result2 = enumerator2.MoveNext();
                   while (result1 && result2)
                   {
                       if (enumerator1.Current.Name != enumerator2.Current.Name)
                       {
                           return false;
                       }

                       if (!(Equals(enumerator1.Current.Value, enumerator2.Current.Value)))
                       {
                           return false;
                       }

                       result1 = enumerator1.MoveNext();
                       result2 = enumerator2.MoveNext();
                   }

                   return result1 == false && result2 == false;
               }

               default:
                   throw new InvalidOperationException($"Unknown JmesPathType {lhs.Type}");
           }
       }

       public int GetHashCode(IValue obj)
       {
           return ComputeHashCode(obj, 0);
       }

       private int ComputeHashCode(IValue element, int depth)
       {
           var hashCode = element.Type.GetHashCode();

           switch (element.Type)
           {
               case JmesPathType.Null:
               case JmesPathType.True:
               case JmesPathType.False:
                   break;

               case JmesPathType.Number:
                    {
                        element.TryGetDouble(out var dbl);
                        hashCode += 17 * dbl.GetHashCode();
                        break;
                    }

               case JmesPathType.String:
                    hashCode += 17 * element.GetString().GetHashCode();
                   break;

               case JmesPathType.Array:
                   if (depth < _maxHashDepth)
                       foreach (var item in element.EnumerateArray())
                           hashCode += 17*ComputeHashCode(item, depth+1);
                   break;

                case JmesPathType.Object:
                    foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        hashCode += 17*property.Name.GetHashCode();
                        if (depth < _maxHashDepth)
                            hashCode += 17*ComputeHashCode(property.Value, depth+1);
                    }
                    break;

                default:
                   throw new InvalidOperationException($"Unknown JmesPathType {element.Type}");
           }
           return hashCode;
       }
   }


}

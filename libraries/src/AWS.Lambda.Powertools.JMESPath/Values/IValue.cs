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

using AWS.Lambda.Powertools.JMESPath.Expressions;

namespace AWS.Lambda.Powertools.JMESPath.Values;

internal interface IValue
{
    /// <summary>
    /// The type of the JMESPath value
    /// </summary>
    JmesPathType Type { get; }
    
    /// <summary>
    /// The value of the JMESPath value
    /// </summary>
    /// <param name="index"></param>
    IValue this[int index] { get; }
    
    /// <summary>
    /// The length of the array
    /// </summary>
    /// <returns></returns>
    int GetArrayLength();
    
    /// <summary>
    /// Get the value as a string
    /// </summary>
    /// <returns></returns>
    string GetString();
    
    /// <summary>
    /// Try to get the value as a decimal
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    bool TryGetDecimal(out decimal value);
    
    /// <summary>
    /// Try to get the value as a double
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    bool TryGetDouble(out double value);
    
    /// <summary>
    /// Try to get the property value
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    bool TryGetProperty(string propertyName, out IValue property);
    
    /// <summary>
    /// Enumerate the array values
    /// </summary>
    /// <returns></returns>
    IArrayValueEnumerator EnumerateArray();
    
    
    /// <summary>
    /// Enumerate the object values
    /// </summary>
    IObjectValueEnumerator EnumerateObject();
    
    
    /// <summary>
    /// Get the expression for this value
    /// </summary>
    IExpression GetExpression();
}
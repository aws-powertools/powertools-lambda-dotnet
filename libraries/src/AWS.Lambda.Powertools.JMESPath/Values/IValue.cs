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

namespace AWS.Lambda.Powertools.JMESPath.Values;

internal interface IValue
{
    JmesPathType Type { get; }
    IValue this[int index] { get; }
    int GetArrayLength();
    string GetString();
    bool TryGetDecimal(out decimal value);
    bool TryGetDouble(out double value);
    bool TryGetProperty(string propertyName, out IValue property);
    IArrayValueEnumerator EnumerateArray();
    IObjectValueEnumerator EnumerateObject();
    IExpression GetExpression();
}
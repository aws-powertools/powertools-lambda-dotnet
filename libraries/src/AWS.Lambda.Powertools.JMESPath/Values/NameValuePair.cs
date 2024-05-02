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

namespace AWS.Lambda.Powertools.JMESPath.Values
{
    /// <summary>
    /// Represents a name-value pair.
    /// </summary>
    internal readonly struct NameValuePair
    {
        public string Name { get; }
        public IValue Value { get; }

        public NameValuePair(string name, IValue value)
        {
            Name = name;
            Value = value;
        }
    }
}
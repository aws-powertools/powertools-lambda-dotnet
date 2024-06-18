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

namespace AWS.Lambda.Powertools.JMESPath.Functions;

/// <summary>
/// A registry of built-in functions.
/// </summary>
internal sealed class BuiltInFunctions
{
    internal static BuiltInFunctions Instance { get; } = new();

    private readonly Dictionary<string, IFunction> _functions = new();

    private BuiltInFunctions()
    {
        _functions.Add("abs", new AbsFunction());
        _functions.Add("avg", new AvgFunction());
        _functions.Add("ceil", new CeilFunction());
        _functions.Add("contains", new ContainsFunction());
        _functions.Add("ends_with", new EndsWithFunction());
        _functions.Add("floor", new FloorFunction());
        _functions.Add("join", new JoinFunction());
        _functions.Add("keys", new KeysFunction());
        _functions.Add("length", new LengthFunction());
        _functions.Add("map", new MapFunction());
        _functions.Add("max", new MaxFunction());
        _functions.Add("max_by", new MaxByFunction());
        _functions.Add("merge", new MergeFunction());
        _functions.Add("min", new MinFunction());
        _functions.Add("min_by", new MinByFunction());
        _functions.Add("not_null", new NotNullFunction());
        _functions.Add("reverse", new ReverseFunction());
        _functions.Add("sort", new SortFunction());
        _functions.Add("sort_by", new SortByFunction());
        _functions.Add("starts_with", new StartsWithFunction());
        _functions.Add("sum", new SumFunction());
        _functions.Add("to_array", new ToArrayFunction());
        _functions.Add("to_number", new ToNumberFunction());
        _functions.Add("to_string", new ToStringFunction());
        _functions.Add("type", new TypeFunction());
        _functions.Add("values", new ValuesFunction());
        _functions.Add("powertools_json", new JsonFunction());
        _functions.Add("powertools_base64", new Base64Function());
        _functions.Add("powertools_base64_gzip", new Base64GzipFunction());
    }

    internal bool TryGetFunction(string name, out IFunction func)
    {
        return _functions.TryGetValue(name, out func);
    }
}
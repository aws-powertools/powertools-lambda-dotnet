using System.Collections.Generic;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

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
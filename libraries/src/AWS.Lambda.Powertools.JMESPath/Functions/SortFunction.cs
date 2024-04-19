using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class SortFunction : BaseFunction
{
    internal SortFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Array)
        {
            element = JsonConstants.Null;
            return false;
        }

        if (arg0.GetArrayLength() <= 1)
        {
            element = arg0;
            return true;
        }

        var isNumber1 = arg0[0].Type == JmesPathType.Number;
        var isString1 = arg0[0].Type == JmesPathType.String;
        if (!isNumber1 && !isString1)
        {
            element = JsonConstants.Null;
            return false;
        }

        var comparer = ValueComparer.Instance;

        var list = new List<IValue>();
        foreach (var item in arg0.EnumerateArray())
        {
            var isNumber2 = item.Type == JmesPathType.Number;
            var isString2 = item.Type == JmesPathType.String;
            if (!(isNumber2 == isNumber1 && isString2 == isString1))
            {
                element = JsonConstants.Null;
                return false;
            }

            list.Add(item);
        }

        list.Sort(comparer);
        element = new ArrayValue(list);
        return true;
    }

    public override string ToString()
    {
        return "sort";
    }
}
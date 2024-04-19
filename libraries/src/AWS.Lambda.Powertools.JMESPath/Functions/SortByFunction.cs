using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class SortByFunction : BaseFunction
{
    internal SortByFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
        {
            element = JsonConstants.Null;
            return false;
        }

        var arg0 = args[0];
        if (arg0.GetArrayLength() <= 1)
        {
            element = arg0;
            return true;
        }

        var expr = args[1].GetExpression();

        var list = new List<IValue>();
        foreach (var item in arg0.EnumerateArray())
        {
            list.Add(item);
        }

        var comparer = new SortByComparer(resources, expr);
        list.Sort(comparer);
        if (comparer.IsValid)
        {
            element = new ArrayValue(list);
            return true;
        }

        element = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "sort_by";
    }
}
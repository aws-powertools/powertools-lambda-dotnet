using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class MapFunction : BaseFunction
{
    internal MapFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        if (!(args[0].Type == JmesPathType.Expression && args[1].Type == JmesPathType.Array))
        {
            element = JsonConstants.Null;
            return false;
        }

        var expr = args[0].GetExpression();
        var arg0 = args[1];

        var list = new List<IValue>();

        foreach (var item in arg0.EnumerateArray())
        {
            if (!expr.TryEvaluate(resources, item, out var val))
            {
                element = JsonConstants.Null;
                return false;
            }

            list.Add(val);
        }

        element = new ArrayValue(list);
        return true;
    }

    public override string ToString()
    {
        return "map";
    }
}
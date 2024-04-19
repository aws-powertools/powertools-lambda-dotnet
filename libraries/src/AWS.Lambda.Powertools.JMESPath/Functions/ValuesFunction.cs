using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class ValuesFunction : BaseFunction
{
    internal ValuesFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Object)
        {
            element = JsonConstants.Null;
            return false;
        }

        var list = new List<IValue>();

        foreach (var item in arg0.EnumerateObject())
        {
            list.Add(item.Value);
        }

        element = new ArrayValue(list);
        return true;
    }

    public override string ToString()
    {
        return "values";
    }
}
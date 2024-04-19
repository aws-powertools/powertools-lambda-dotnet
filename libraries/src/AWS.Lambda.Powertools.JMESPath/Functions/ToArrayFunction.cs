using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class ToArrayFunction : BaseFunction
{
    internal ToArrayFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        if (arg0.Type == JmesPathType.Array)
        {
            element = arg0;
            return true;
        }

        var list = new List<IValue> { arg0 };
        element = new ArrayValue(list);
        return true;
    }

    public override string ToString()
    {
        return "to_array";
    }
}
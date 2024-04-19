using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class AvgFunction : BaseFunction
{
    internal AvgFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Array || arg0.GetArrayLength() == 0)
        {
            element = JsonConstants.Null;
            return false;
        }

        if (!SumFunction.Instance.TryEvaluate(resources, args, out var sum))
        {
            element = JsonConstants.Null;
            return false;
        }

        if (sum.TryGetDecimal(out var decVal))
        {
            element = new DecimalValue(decVal / arg0.GetArrayLength());
            return true;
        }

        if (sum.TryGetDouble(out var dblVal))
        {
            element = new DoubleValue(dblVal / arg0.GetArrayLength());
            return true;
        }

        element = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "avg";
    }
}
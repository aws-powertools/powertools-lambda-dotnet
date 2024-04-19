using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class SumFunction : BaseFunction
{
    internal static SumFunction Instance { get; } = new();

    internal SumFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Array)
        {
            element = JsonConstants.Null;
            return false;
        }

        foreach (var item in arg0.EnumerateArray())
        {
            if (item.Type == JmesPathType.Number) continue;
            element = JsonConstants.Null;
            return false;
        }

        var success = true;
        decimal decSum = 0;
        foreach (var item in arg0.EnumerateArray())
        {
            if (!item.TryGetDecimal(out var dec))
            {
                success = false;
                break;
            }

            decSum += dec;
        }

        if (success)
        {
            element = new DecimalValue(decSum);
            return true;
        }

        double dblSum = 0;
        foreach (var item in arg0.EnumerateArray())
        {
            if (!item.TryGetDouble(out var dbl))
            {
                element = JsonConstants.Null;
                return false;
            }

            dblSum += dbl;
        }

        element = new DoubleValue(dblSum);
        return true;
    }

    public override string ToString()
    {
        return "sum";
    }
}
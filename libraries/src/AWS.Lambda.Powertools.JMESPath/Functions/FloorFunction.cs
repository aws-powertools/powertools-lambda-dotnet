using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class FloorFunction : BaseFunction
{
    internal FloorFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var val = args[0];
        if (val.Type != JmesPathType.Number)
        {
            element = JsonConstants.Null;
            return false;
        }

        if (val.TryGetDecimal(out var decVal))
        {
            element = new DecimalValue(decimal.Floor(decVal));
            return true;
        }

        if (val.TryGetDouble(out var dblVal))
        {
            element = new DoubleValue(Math.Floor(dblVal));
            return true;
        }

        element = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "floor";
    }
}
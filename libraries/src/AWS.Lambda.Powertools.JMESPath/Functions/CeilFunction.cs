using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class CeilFunction : BaseFunction
{
    internal CeilFunction()
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
            element = new DecimalValue(decimal.Ceiling(decVal));
            return true;
        }

        if (val.TryGetDouble(out var dblVal))
        {
            element = new DoubleValue(Math.Ceiling(dblVal));
            return true;
        }

        element = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "ceil";
    }
}
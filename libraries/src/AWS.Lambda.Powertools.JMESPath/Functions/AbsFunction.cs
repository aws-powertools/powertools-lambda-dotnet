using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class AbsFunction : BaseFunction
{
    internal AbsFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg = args[0];

        if (arg.TryGetDecimal(out var decVal))
        {
            element = new DecimalValue(decVal >= 0 ? decVal : -decVal);
            return true;
        }

        if (arg.TryGetDouble(out var dblVal))
        {
            element = new DecimalValue(dblVal >= 0 ? decVal : new decimal(-dblVal));
            return true;
        }

        element = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "abs";
    }
}
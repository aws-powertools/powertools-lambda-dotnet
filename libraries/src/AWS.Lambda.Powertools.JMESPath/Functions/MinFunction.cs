using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class MinFunction : BaseFunction
{
    internal MinFunction()
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

        if (arg0.GetArrayLength() == 0)
        {
            element = JsonConstants.Null;
            return false;
        }

        var isNumber = arg0[0].Type == JmesPathType.Number;
        var isString = arg0[0].Type == JmesPathType.String;
        if (!isNumber && !isString)
        {
            element = JsonConstants.Null;
            return false;
        }

        var less = LtOperator.Instance;
        var index = 0;
        for (var i = 1; i < arg0.GetArrayLength(); ++i)
        {
            if (!(arg0[i].Type == JmesPathType.Number == isNumber &&
                  arg0[i].Type == JmesPathType.String == isString))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (!less.TryEvaluate(arg0[i], arg0[index], out var value))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (value.Type == JmesPathType.True)
            {
                index = i;
            }
        }

        element = arg0[index];
        return true;
    }

    public override string ToString()
    {
        return "min";
    }
}
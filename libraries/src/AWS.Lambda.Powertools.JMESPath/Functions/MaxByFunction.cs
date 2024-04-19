using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class MaxByFunction : BaseFunction
{
    internal MaxByFunction()
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
        if (arg0.GetArrayLength() == 0)
        {
            element = JsonConstants.Null;
            return true;
        }

        var expr = args[1].GetExpression();

        if (!expr.TryEvaluate(resources, arg0[0], out var key1))
        {
            element = JsonConstants.Null;
            return false;
        }

        var isNumber1 = key1.Type == JmesPathType.Number;
        var isString1 = key1.Type == JmesPathType.String;
        if (!(isNumber1 || isString1))
        {
            element = JsonConstants.Null;
            return false;
        }

        var greater = GtOperator.Instance;
        var index = 0;
        for (var i = 1; i < arg0.GetArrayLength(); ++i)
        {
            if (!expr.TryEvaluate(resources, arg0[i], out var key2))
            {
                element = JsonConstants.Null;
                return false;
            }

            var isNumber2 = key2.Type == JmesPathType.Number;
            var isString2 = key2.Type == JmesPathType.String;
            if (!(isNumber2 == isNumber1 && isString2 == isString1))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (!greater.TryEvaluate(key2, key1, out var value))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (value.Type != JmesPathType.True) continue;
            key1 = key2;
            index = i;
        }

        element = arg0[index];
        return true;
    }

    public override string ToString()
    {
        return "max_by";
    }
}
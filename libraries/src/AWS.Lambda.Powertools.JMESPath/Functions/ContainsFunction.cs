using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class ContainsFunction : BaseFunction
{
    internal ContainsFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        var arg1 = args[1];

        var comparer = ValueEqualityComparer.Instance;

        switch (arg0.Type)
        {
            case JmesPathType.Array:
                if (arg0.EnumerateArray().Any(item => comparer.Equals(item, arg1)))
                {
                    element = JsonConstants.True;
                    return true;
                }

                element = JsonConstants.False;
                return true;
            case JmesPathType.String:
            {
                if (arg1.Type != JmesPathType.String)
                {
                    element = JsonConstants.Null;
                    return false;
                }

                var s0 = arg0.GetString();
                var s1 = arg1.GetString();
                if (s0.Contains(s1))
                {
                    element = JsonConstants.True;
                    return true;
                }

                element = JsonConstants.False;
                return true;
            }
            default:
            {
                element = JsonConstants.Null;
                return false;
            }
        }
    }

    public override string ToString()
    {
        return "contains";
    }
}
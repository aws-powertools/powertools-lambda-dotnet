using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class EndsWithFunction : BaseFunction
{
    internal EndsWithFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        var arg1 = args[1];
        if (arg0.Type != JmesPathType.String
            || arg1.Type != JmesPathType.String)
        {
            element = JsonConstants.Null;
            return false;
        }

        var s0 = arg0.GetString();
        var s1 = arg1.GetString();

        element = s0.EndsWith(s1) ? JsonConstants.True : JsonConstants.False;

        return true;
    }

    public override string ToString()
    {
        return "ends_with";
    }
}
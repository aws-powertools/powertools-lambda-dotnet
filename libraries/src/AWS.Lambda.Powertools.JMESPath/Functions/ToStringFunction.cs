using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class ToStringFunction : BaseFunction
{
    internal ToStringFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        if (args[0].Type == JmesPathType.Expression)
        {
            element = JsonConstants.Null;
            return false;
        }

        var arg0 = args[0];
        switch (arg0.Type)
        {
            case JmesPathType.String:
                element = arg0;
                return true;
            case JmesPathType.Expression:
                element = JsonConstants.Null;
                return false;
            default:
                element = new StringValue(arg0.ToString());
                return true;
        }
    }

    public override string ToString()
    {
        return "to_string";
    }
}
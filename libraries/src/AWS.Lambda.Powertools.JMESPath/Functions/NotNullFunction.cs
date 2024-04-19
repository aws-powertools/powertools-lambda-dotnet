using System.Collections.Generic;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class NotNullFunction : BaseFunction
{
    internal NotNullFunction()
        : base(null)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        foreach (var arg in args)
        {
            if (arg.Type == JmesPathType.Null) continue;
            element = arg;
            return true;
        }

        element = JsonConstants.Null;
        return true;
    }

    public override string ToString()
    {
        return "not_null";
    }
}
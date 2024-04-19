using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class TypeFunction : BaseFunction
{
    internal TypeFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];

        switch (arg0.Type)
        {
            case JmesPathType.Number:
                element = new StringValue("number");
                return true;
            case JmesPathType.True:
            case JmesPathType.False:
                element = new StringValue("boolean");
                return true;
            case JmesPathType.String:
                element = new StringValue("string");
                return true;
            case JmesPathType.Object:
                element = new StringValue("object");
                return true;
            case JmesPathType.Array:
                element = new StringValue("array");
                return true;
            case JmesPathType.Null:
                element = new StringValue("null");
                return true;
            default:
                element = JsonConstants.Null;
                return false;
        }
    }

    public override string ToString()
    {
        return "type";
    }
}
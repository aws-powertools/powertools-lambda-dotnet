using System.Collections.Generic;
using System.Diagnostics;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class JsonFunction : BaseFunction
{
    /// <inheritdoc />
    public JsonFunction()
        : base(1)
    {
    }

    public override string ToString()
    {
        return "powertools_json";
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);
        element = args[0];
        return true;
    }
}
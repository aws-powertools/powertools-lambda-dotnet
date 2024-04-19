using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class Base64Function : BaseFunction
{
    /// <inheritdoc />
    public Base64Function()
        : base(1)
    {
    }

    public override string ToString()
    {
        return "powertools_base64";
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);
        var base64StringBytes = Convert.FromBase64String(args[0].GetString());
        var doc = JsonDocument.Parse(base64StringBytes);
        element = new JsonElementValue(doc.RootElement);
        return true;
    }
}
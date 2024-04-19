using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class ToNumberFunction : BaseFunction
{
    internal ToNumberFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
        out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        switch (arg0.Type)
        {
            case JmesPathType.Number:
                element = arg0;
                return true;
            case JmesPathType.String:
            {
                var s = arg0.GetString();
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                {
                    element = new DecimalValue(dec);
                    return true;
                }

                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl))
                {
                    element = new DoubleValue(dbl);
                    return true;
                }

                element = JsonConstants.Null;
                return false;
            }
            default:
                element = JsonConstants.Null;
                return false;
        }
    }

    public override string ToString()
    {
        return "to_number";
    }
}
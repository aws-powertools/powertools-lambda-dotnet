using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class LengthFunction : BaseFunction
{
    internal LengthFunction()
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
            case JmesPathType.Object:
            {
                var count = 0;
                foreach (var unused in arg0.EnumerateObject())
                {
                    ++count;
                }

                element = new DecimalValue(new decimal(count));
                return true;
            }
            case JmesPathType.Array:
                element = new DecimalValue(new decimal(arg0.GetArrayLength()));
                return true;
            case JmesPathType.String:
            {
                var bytes = Encoding.UTF32.GetBytes(arg0.GetString().ToCharArray());
                element = new DecimalValue(new decimal(bytes.Length / 4));
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
        return "length";
    }
}
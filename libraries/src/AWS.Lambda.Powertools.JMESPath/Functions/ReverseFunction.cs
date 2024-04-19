using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class ReverseFunction : BaseFunction
{
    internal ReverseFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

        var arg0 = args[0];
        switch (arg0.Type)
        {
            case JmesPathType.String:
            {
                element = new StringValue(string.Join("", GraphemeClusters(arg0.GetString()).Reverse().ToArray()));
                return true;
            }
            case JmesPathType.Array:
            {
                var list = new List<IValue>();
                for (var i = arg0.GetArrayLength() - 1; i >= 0; --i)
                {
                    list.Add(arg0[i]);
                }

                element = new ArrayValue(list);
                return true;
            }
            default:
                element = JsonConstants.Null;
                return false;
        }
    }

    private static IEnumerable<string> GraphemeClusters(string s)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(s);
        while (enumerator.MoveNext())
        {
            yield return (string)enumerator.Current;
        }
    }

    public override string ToString()
    {
        return "reverse";
    }
}
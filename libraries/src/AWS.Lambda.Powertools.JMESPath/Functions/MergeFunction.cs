using System.Collections.Generic;
using System.Linq;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal sealed class MergeFunction : BaseFunction
{
    internal MergeFunction()
        : base(null)
    {
    }

    public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
    {
        if (!args.Any())
        {
            element = JsonConstants.Null;
            return false;
        }

        var arg0 = args[0];
        if (arg0.Type != JmesPathType.Object)
        {
            element = JsonConstants.Null;
            return false;
        }

        if (args.Count == 1)
        {
            element = arg0;
            return true;
        }

        var dict = new Dictionary<string, IValue>();
        foreach (var argi in args)
        {
            if (argi.Type != JmesPathType.Object)
            {
                element = JsonConstants.Null;
                return false;
            }

            foreach (var item in argi.EnumerateObject())
            {
                if (dict.TryAdd(item.Name, item.Value)) continue;
                dict.Remove(item.Name);
                dict.Add(item.Name, item.Value);
            }
        }

        element = new ObjectValue(dict);
        return true;
    }

    public override string ToString()
    {
        return "merge";
    }
}
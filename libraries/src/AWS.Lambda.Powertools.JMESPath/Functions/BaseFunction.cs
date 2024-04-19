using System.Collections.Generic;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal abstract class BaseFunction : IFunction
{
    private protected BaseFunction(int? argCount)
    {
        Arity = argCount;
    }

    public int? Arity { get; }

    public abstract bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element);
}
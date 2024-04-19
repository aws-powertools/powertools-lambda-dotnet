using System.Collections.Generic;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

internal interface IFunction
{
    int? Arity { get; }
    bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element);
}
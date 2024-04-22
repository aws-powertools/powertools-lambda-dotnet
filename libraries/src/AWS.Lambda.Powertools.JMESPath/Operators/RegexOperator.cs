using System.Text.RegularExpressions;
using AWS.Lambda.Powertools.JMESPath.Expressions;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Operators;

internal sealed class RegexOperator : UnaryOperator
{
    private readonly Regex _regex;

    internal RegexOperator(Regex regex)
        : base(Operator.Not)
    {
        _regex = regex;
    }

    public override bool TryEvaluate(IValue elem, out IValue result)
    {
        if (elem.Type != JmesPathType.String)
        {
            result = JsonConstants.Null;
            return false; // type error
        }
        result = _regex.IsMatch(elem.GetString()) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "Regex";
    }
}
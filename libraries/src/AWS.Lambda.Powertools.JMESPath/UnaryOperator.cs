using System.Text.RegularExpressions;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal interface IUnaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        bool TryEvaluate(IValue elem, out IValue result);
    };

    internal abstract class UnaryOperator : IUnaryOperator
    {
        internal UnaryOperator(Operator oper)
        {
            PrecedenceLevel = OperatorTable.PrecedenceLevel(oper);
            IsRightAssociative = OperatorTable.IsRightAssociative(oper);
        }

        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative {get;} 

        public abstract bool TryEvaluate(IValue elem, out IValue result);
    };

    internal sealed class NotOperator : UnaryOperator
    {
        internal static NotOperator Instance { get; } = new();

        internal NotOperator()
            : base(Operator.Not)
        {}

        public override bool TryEvaluate(IValue val, out IValue result)
        {
            result = Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "Not";
        }
    };

    internal sealed class RegexOperator : UnaryOperator
    {
        private Regex _regex;

        internal RegexOperator(Regex regex)
            : base(Operator.Not)
        {
            _regex = regex;
        }

        public override bool TryEvaluate(IValue val, out IValue result)
        {
            if (!(val.Type == JmesPathType.String))
            {
                result = JsonConstants.Null;
                return false; // type error
            }
            result = _regex.IsMatch(val.GetString()) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "Regex";
        }
    };

}


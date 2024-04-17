using System;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal interface IBinaryOperator 
    {
        int PrecedenceLevel {get;}
        bool IsRightAssociative {get;}
        bool TryEvaluate(IValue lhs, IValue rhs, out IValue result);
    };

    internal abstract class BinaryOperator : IBinaryOperator
    {
        internal BinaryOperator(Operator oper)
        {
            PrecedenceLevel = OperatorTable.PrecedenceLevel(oper);
        }

        public int PrecedenceLevel {get;} 

        public bool IsRightAssociative => false;

        public abstract bool TryEvaluate(IValue lhs, IValue rhs, out IValue result);
    };

    internal sealed class OrOperator : BinaryOperator
    {
        internal static OrOperator Instance { get; } = new();

        private OrOperator()
            : base(Operator.Or)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
        {
            if (lhs.Type == JmesPathType.Null && rhs.Type == JmesPathType.Null)
            {
                result = lhs;
                return true;
            }
            result = Expression.IsTrue(lhs) ? lhs : rhs;
            return true;
        }

        public override string ToString()
        {
            return "OrOperator";
        }
    };

    internal sealed class AndOperator : BinaryOperator
    {
        internal static AndOperator Instance { get; } = new();

        private AndOperator()
            : base(Operator.And)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
        {
            result = Expression.IsTrue(lhs) ? rhs : lhs;
            return true;
        }

        public override string ToString()
        {
            return "AndOperator";
        }
    };

    internal sealed class EqOperator : BinaryOperator
    {
        internal static EqOperator Instance { get; } = new();

        private EqOperator()
            : base(Operator.Eq)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
        {
            var comparer = ValueEqualityComparer.Instance;
            result = comparer.Equals(lhs, rhs) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "EqOperator";
        }
    };

    internal sealed class NeOperator : BinaryOperator
    {
        internal static NeOperator Instance { get; } = new();

        private NeOperator()
            : base(Operator.Ne)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
        {
            if (!EqOperator.Instance.TryEvaluate(lhs, rhs, out var value))
            {
                result = JsonConstants.Null;
                return false;
            }
                
            result = Expression.IsFalse(value) ? JsonConstants.True : JsonConstants.False;
            return true;
        }

        public override string ToString()
        {
            return "NeOperator";
        }
    };

    internal sealed class LtOperator : BinaryOperator
    {
        internal static LtOperator Instance { get; } = new();

        private LtOperator()
            : base(Operator.Lt)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
        {
            if (lhs.Type == JmesPathType.Number && rhs.Type == JmesPathType.Number)
            {
                if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
                {
                    result = dec1 < dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
                {
                    result = val1 < val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }
            }
            else if (lhs.Type == JmesPathType.String && rhs.Type == JmesPathType.String)
            {
                result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) < 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return "LtOperator";
        }
    };

    internal sealed class LteOperator : BinaryOperator
    {
        internal static LteOperator Instance { get; } = new();

        private LteOperator()
            : base(Operator.Lte)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
        {
            if (lhs.Type == JmesPathType.Number && rhs.Type == JmesPathType.Number)
            {
                if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
                {
                    result = dec1 <= dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
                {
                    result = val1 <= val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }
            }
            else if (lhs.Type == JmesPathType.String && rhs.Type == JmesPathType.String)
            {
                result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) <= 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
            return true;
        }


        public override string ToString()
        {
            return "LteOperator";
        }
    };

    internal sealed class GtOperator : BinaryOperator
    {
        internal static GtOperator Instance { get; } = new();

        private GtOperator()
            : base(Operator.Gt)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
        {
            if (lhs.Type == JmesPathType.Number && rhs.Type == JmesPathType.Number)
            {
                if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
                {
                    result = dec1 > dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
                {
                    result = val1 > val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }
            }
            else if (lhs.Type == JmesPathType.String && rhs.Type == JmesPathType.String)
            {
                result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) > 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return "GtOperator";
        }
    };

    internal sealed class GteOperator : BinaryOperator
    {
        internal static GteOperator Instance { get; } = new();

        private GteOperator()
            : base(Operator.Gte)
        {
        }

        public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
        {
            if (lhs.Type == JmesPathType.Number && rhs.Type == JmesPathType.Number)
            {
                if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
                {
                    result = dec1 >= dec2 ? JsonConstants.True : JsonConstants.False;
                }
                else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
                {
                    result = val1 >= val2 ? JsonConstants.True : JsonConstants.False;
                }
                else
                {
                    result = JsonConstants.Null;
                }
            }
            else if (lhs.Type == JmesPathType.String && rhs.Type == JmesPathType.String)
            {
                result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) >= 0 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
            return true;
        }

        public override string ToString()
        {
            return "GteOperator";
        }
    };
}


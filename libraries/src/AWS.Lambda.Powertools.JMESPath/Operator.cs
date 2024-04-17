namespace AWS.Lambda.Powertools.JMESPath
{
    internal enum Operator
    {
        Default, // Identifier, CurrentNode, Index, MultiSelectList, MultiSelectHash, FunctionExpression
        Projection,
        FlattenProjection, // FlattenProjection
        Or,
        And,
        Eq,
        Ne,
        Lt,
        Lte,
        Gt,
        Gte,
        Not
    }

    internal static class OperatorTable
    {
        static internal int PrecedenceLevel(Operator oper)
        {
            switch (oper)
            {
                case Operator.Projection:
                    return 1;
                case Operator.FlattenProjection:
                    return 1;
                case Operator.Or:
                    return 2;
                case Operator.And:
                    return 3;
                case Operator.Eq:
                case Operator.Ne:
                    return 4;
                case Operator.Lt:
                case Operator.Lte:
                case Operator.Gt:
                case Operator.Gte:
                    return 5;
                case Operator.Not:
                    return 6;
                default:
                    return 6;
            }
        }

        static internal bool IsRightAssociative(Operator oper)
        {
            switch (oper)
            {
                case Operator.Not:
                    return true;
                case Operator.Projection:
                    return true;
                case Operator.FlattenProjection:
                    return false;
                case Operator.Or:
                case Operator.And:
                case Operator.Eq:
                case Operator.Ne:
                case Operator.Lt:
                case Operator.Lte:
                case Operator.Gt:
                case Operator.Gte:
                    return false;
                default:
                    return false;
            }
        }
    }

}


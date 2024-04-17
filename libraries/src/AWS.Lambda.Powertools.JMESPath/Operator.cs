/*
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

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
        internal static int PrecedenceLevel(Operator oper)
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

        internal static bool IsRightAssociative(Operator oper)
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


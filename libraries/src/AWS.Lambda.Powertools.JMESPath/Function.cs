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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AWS.Lambda.Powertools.JMESPath
{
    internal sealed class SortByComparer : IComparer<IValue>, System.Collections.IComparer
    {
        private readonly DynamicResources _resources;
        private readonly IExpression _expr;

        internal bool IsValid { get; set; } = true;

        internal SortByComparer(DynamicResources resources,
            IExpression expr)
        {
            _resources = resources;
            _expr = expr;
        }

        public int Compare(IValue lhs, IValue rhs)
        {
            var comparer = ValueComparer.Instance;

            if (!IsValid)
            {
                return 0;
            }

            if (!_expr.TryEvaluate(_resources, lhs, out var key1))
            {
                IsValid = false;
                return 0;
            }

            var isNumber1 = key1.Type == JmesPathType.Number;
            var isString1 = key1.Type == JmesPathType.String;
            if (!(isNumber1 || isString1))
            {
                IsValid = false;
                return 0;
            }

            if (!_expr.TryEvaluate(_resources, rhs, out var key2))
            {
                IsValid = false;
                return 0;
            }

            var isNumber2 = key2.Type == JmesPathType.Number;
            var isString2 = key2.Type == JmesPathType.String;
            if (isNumber2 == isNumber1 && isString2 == isString1) return comparer.Compare(key1, key2);
            IsValid = false;
            return 0;
        }

        int System.Collections.IComparer.Compare(object x, object y)
        {
            return Compare((IValue)x, (IValue)y);
        }
    }

    internal interface IFunction
    {
        int? Arity { get; }
        bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element);
    }

    internal abstract class BaseFunction : IFunction
    {
        internal BaseFunction(int? argCount)
        {
            Arity = argCount;
        }

        public int? Arity { get; }

        public abstract bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element);
    }

    internal sealed class AbsFunction : BaseFunction
    {
        internal AbsFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg = args[0];

            if (arg.TryGetDecimal(out var decVal))
            {
                element = new DecimalValue(decVal >= 0 ? decVal : -decVal);
                return true;
            }

            if (arg.TryGetDouble(out var dblVal))
            {
                element = new DecimalValue(dblVal >= 0 ? decVal : new decimal(-dblVal));
                return true;
            }

            element = JsonConstants.Null;
            return false;
        }

        public override string ToString()
        {
            return "abs";
        }
    }

    internal sealed class AvgFunction : BaseFunction
    {
        internal AvgFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array || arg0.GetArrayLength() == 0)
            {
                element = JsonConstants.Null;
                return false;
            }

            if (!SumFunction.Instance.TryEvaluate(resources, args, out var sum))
            {
                element = JsonConstants.Null;
                return false;
            }

            if (sum.TryGetDecimal(out var decVal))
            {
                element = new DecimalValue(decVal / arg0.GetArrayLength());
                return true;
            }

            if (sum.TryGetDouble(out var dblVal))
            {
                element = new DoubleValue(dblVal / arg0.GetArrayLength());
                return true;
            }

            element = JsonConstants.Null;
            return false;
        }

        public override string ToString()
        {
            return "avg";
        }
    }

    internal sealed class CeilFunction : BaseFunction
    {
        internal CeilFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var val = args[0];
            if (val.Type != JmesPathType.Number)
            {
                element = JsonConstants.Null;
                return false;
            }

            if (val.TryGetDecimal(out var decVal))
            {
                element = new DecimalValue(decimal.Ceiling(decVal));
                return true;
            }

            if (val.TryGetDouble(out var dblVal))
            {
                element = new DoubleValue(Math.Ceiling(dblVal));
                return true;
            }

            element = JsonConstants.Null;
            return false;
        }

        public override string ToString()
        {
            return "ceil";
        }
    }

    internal sealed class ContainsFunction : BaseFunction
    {
        internal ContainsFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            var arg1 = args[1];

            var comparer = ValueEqualityComparer.Instance;

            switch (arg0.Type)
            {
                case JmesPathType.Array:
                    foreach (var item in arg0.EnumerateArray())
                    {
                        if (comparer.Equals(item, arg1))
                        {
                            element = JsonConstants.True;
                            return true;
                        }
                    }

                    element = JsonConstants.False;
                    return true;
                case JmesPathType.String:
                {
                    if (arg1.Type != JmesPathType.String)
                    {
                        element = JsonConstants.Null;
                        return false;
                    }

                    var s0 = arg0.GetString();
                    var s1 = arg1.GetString();
                    if (s0.Contains(s1))
                    {
                        element = JsonConstants.True;
                        return true;
                    }

                    element = JsonConstants.False;
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
            return "contains";
        }
    }

    internal sealed class EndsWithFunction : BaseFunction
    {
        internal EndsWithFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            var arg1 = args[1];
            if (arg0.Type != JmesPathType.String
                || arg1.Type != JmesPathType.String)
            {
                element = JsonConstants.Null;
                return false;
            }

            var s0 = arg0.GetString();
            var s1 = arg1.GetString();

            if (s0.EndsWith(s1))
            {
                element = JsonConstants.True;
            }
            else
            {
                element = JsonConstants.False;
            }

            return true;
        }

        public override string ToString()
        {
            return "ends_with";
        }
    }

    internal sealed class FloorFunction : BaseFunction
    {
        internal FloorFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var val = args[0];
            if (val.Type != JmesPathType.Number)
            {
                element = JsonConstants.Null;
                return false;
            }

            if (val.TryGetDecimal(out var decVal))
            {
                element = new DecimalValue(decimal.Floor(decVal));
                return true;
            }

            if (val.TryGetDouble(out var dblVal))
            {
                element = new DoubleValue(Math.Floor(dblVal));
                return true;
            }

            element = JsonConstants.Null;
            return false;
        }

        public override string ToString()
        {
            return "floor";
        }
    }

    internal sealed class JoinFunction : BaseFunction
    {
        internal JoinFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            var arg1 = args[1];

            if (!(arg0.Type == JmesPathType.String && args[1].Type == JmesPathType.Array))
            {
                element = JsonConstants.Null;
                return false;
            }

            var sep = arg0.GetString();
            var buf = new StringBuilder();
            foreach (var j in arg1.EnumerateArray())
            {
                if (j.Type != JmesPathType.String)
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (buf.Length != 0)
                {
                    buf.Append(sep);
                }

                var sv = j.GetString();
                buf.Append(sv);
            }

            element = new StringValue(buf.ToString());
            return true;
        }

        public override string ToString()
        {
            return "join";
        }
    }

    internal sealed class KeysFunction : BaseFunction
    {
        internal KeysFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Object)
            {
                element = JsonConstants.Null;
                return false;
            }

            var values = new List<IValue>();

            foreach (var property in arg0.EnumerateObject())
            {
                values.Add(new StringValue(property.Name));
            }

            element = new ArrayValue(values);
            return true;
        }

        public override string ToString()
        {
            return "keys";
        }
    }

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

    internal sealed class MaxFunction : BaseFunction
    {
        internal MaxFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array)
            {
                element = JsonConstants.Null;
                return false;
            }

            if (arg0.GetArrayLength() == 0)
            {
                element = JsonConstants.Null;
                return false;
            }

            var isNumber = arg0[0].Type == JmesPathType.Number;
            var isString = arg0[0].Type == JmesPathType.String;
            if (!isNumber && !isString)
            {
                element = JsonConstants.Null;
                return false;
            }

            var greater = GtOperator.Instance;
            var index = 0;
            for (var i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (!(((arg0[i].Type == JmesPathType.Number) == isNumber) &&
                      (arg0[i].Type == JmesPathType.String) == isString))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (!greater.TryEvaluate(arg0[i], arg0[index], out var value))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (Expression.IsTrue(value))
                {
                    index = i;
                }
            }

            element = arg0[index];
            return true;
        }

        public override string ToString()
        {
            return "max";
        }
    }

    internal sealed class MaxByFunction : BaseFunction
    {
        internal MaxByFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
            {
                element = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (arg0.GetArrayLength() == 0)
            {
                element = JsonConstants.Null;
                return true;
            }

            var expr = args[1].GetExpression();

            if (!expr.TryEvaluate(resources, arg0[0], out var key1))
            {
                element = JsonConstants.Null;
                return false;
            }

            var isNumber1 = key1.Type == JmesPathType.Number;
            var isString1 = key1.Type == JmesPathType.String;
            if (!(isNumber1 || isString1))
            {
                element = JsonConstants.Null;
                return false;
            }

            var greater = GtOperator.Instance;
            var index = 0;
            for (var i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (!expr.TryEvaluate(resources, arg0[i], out var key2))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                var isNumber2 = key2.Type == JmesPathType.Number;
                var isString2 = key2.Type == JmesPathType.String;
                if (!(isNumber2 == isNumber1 && isString2 == isString1))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (!greater.TryEvaluate(key2, key1, out var value))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (value.Type != JmesPathType.True) continue;
                key1 = key2;
                index = i;
            }

            element = arg0[index];
            return true;
        }

        public override string ToString()
        {
            return "max_by";
        }
    }

    internal sealed class MinFunction : BaseFunction
    {
        internal MinFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array)
            {
                element = JsonConstants.Null;
                return false;
            }

            if (arg0.GetArrayLength() == 0)
            {
                element = JsonConstants.Null;
                return false;
            }

            var isNumber = arg0[0].Type == JmesPathType.Number;
            var isString = arg0[0].Type == JmesPathType.String;
            if (!isNumber && !isString)
            {
                element = JsonConstants.Null;
                return false;
            }

            var less = LtOperator.Instance;
            var index = 0;
            for (var i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (!(((arg0[i].Type == JmesPathType.Number) == isNumber) &&
                      (arg0[i].Type == JmesPathType.String) == isString))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (!less.TryEvaluate(arg0[i], arg0[index], out var value))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (value.Type == JmesPathType.True)
                {
                    index = i;
                }
            }

            element = arg0[index];
            return true;
        }

        public override string ToString()
        {
            return "min";
        }
    }

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
            for (var i = 0; i < args.Count; ++i)
            {
                var argi = args[i];
                if (argi.Type != JmesPathType.Object)
                {
                    element = JsonConstants.Null;
                    return false;
                }

                foreach (var item in argi.EnumerateObject())
                {
                    if (!dict.TryAdd(item.Name, item.Value))
                    {
                        dict.Remove(item.Name);
                        dict.Add(item.Name, item.Value);
                    }
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

    internal sealed class NotNullFunction : BaseFunction
    {
        internal NotNullFunction()
            : base(null)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            foreach (var arg in args)
            {
                if (arg.Type == JmesPathType.Null) continue;
                element = arg;
                return true;
            }

            element = JsonConstants.Null;
            return true;
        }

        public override string ToString()
        {
            return "not_null";
        }
    }

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

    internal sealed class MapFunction : BaseFunction
    {
        internal MapFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            if (!(args[0].Type == JmesPathType.Expression && args[1].Type == JmesPathType.Array))
            {
                element = JsonConstants.Null;
                return false;
            }

            var expr = args[0].GetExpression();
            var arg0 = args[1];

            var list = new List<IValue>();

            foreach (var item in arg0.EnumerateArray())
            {
                if (!expr.TryEvaluate(resources, item, out var val))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                list.Add(val);
            }

            element = new ArrayValue(list);
            return true;
        }

        public override string ToString()
        {
            return "map";
        }
    }

    internal sealed class MinByFunction : BaseFunction
    {
        internal MinByFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
            {
                element = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (arg0.GetArrayLength() == 0)
            {
                element = JsonConstants.Null;
                return true;
            }

            var expr = args[1].GetExpression();

            if (!expr.TryEvaluate(resources, arg0[0], out var key1))
            {
                element = JsonConstants.Null;
                return false;
            }

            var isNumber1 = key1.Type == JmesPathType.Number;
            var isString1 = key1.Type == JmesPathType.String;
            if (!(isNumber1 || isString1))
            {
                element = JsonConstants.Null;
                return false;
            }

            var lessor = LtOperator.Instance;
            var index = 0;
            for (var i = 1; i < arg0.GetArrayLength(); ++i)
            {
                if (!expr.TryEvaluate(resources, arg0[i], out var key2))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                var isNumber2 = key2.Type == JmesPathType.Number;
                var isString2 = key2.Type == JmesPathType.String;
                if (!(isNumber2 == isNumber1 && isString2 == isString1))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (!lessor.TryEvaluate(key2, key1, out var value))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                if (value.Type == JmesPathType.True)
                {
                    key1 = key2;
                    index = i;
                }
            }

            element = arg0[index];
            return true;
        }

        public override string ToString()
        {
            return "min_by";
        }
    }

    internal sealed class SortFunction : BaseFunction
    {
        internal SortFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array)
            {
                element = JsonConstants.Null;
                return false;
            }

            if (arg0.GetArrayLength() <= 1)
            {
                element = arg0;
                return true;
            }

            var isNumber1 = arg0[0].Type == JmesPathType.Number;
            var isString1 = arg0[0].Type == JmesPathType.String;
            if (!isNumber1 && !isString1)
            {
                element = JsonConstants.Null;
                return false;
            }

            var comparer = ValueComparer.Instance;

            var list = new List<IValue>();
            foreach (var item in arg0.EnumerateArray())
            {
                var isNumber2 = item.Type == JmesPathType.Number;
                var isString2 = item.Type == JmesPathType.String;
                if (!(isNumber2 == isNumber1 && isString2 == isString1))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                list.Add(item);
            }

            list.Sort(comparer);
            element = new ArrayValue(list);
            return true;
        }

        public override string ToString()
        {
            return "sort";
        }
    }

    internal sealed class SortByFunction : BaseFunction
    {
        internal SortByFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            if (!(args[0].Type == JmesPathType.Array && args[1].Type == JmesPathType.Expression))
            {
                element = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            if (arg0.GetArrayLength() <= 1)
            {
                element = arg0;
                return true;
            }

            var expr = args[1].GetExpression();

            var list = new List<IValue>();
            foreach (var item in arg0.EnumerateArray())
            {
                list.Add(item);
            }

            var comparer = new SortByComparer(resources, expr);
            list.Sort(comparer);
            if (comparer.IsValid)
            {
                element = new ArrayValue(list);
                return true;
            }

            element = JsonConstants.Null;
            return false;
        }

        public override string ToString()
        {
            return "sort_by";
        }
    }

    internal sealed class StartsWithFunction : BaseFunction
    {
        internal StartsWithFunction()
            : base(2)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            var arg1 = args[1];
            if (arg0.Type != JmesPathType.String
                || arg1.Type != JmesPathType.String)
            {
                element = JsonConstants.Null;
                return false;
            }

            var s0 = arg0.GetString();
            var s1 = arg1.GetString();
            element = s0.StartsWith(s1) ? JsonConstants.True : JsonConstants.False;

            return true;
        }

        public override string ToString()
        {
            return "starts_with";
        }
    }

    internal sealed class SumFunction : BaseFunction
    {
        internal static SumFunction Instance { get; } = new();

        internal SumFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args,
            out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Array)
            {
                element = JsonConstants.Null;
                return false;
            }

            foreach (var item in arg0.EnumerateArray())
            {
                if (item.Type != JmesPathType.Number)
                {
                    element = JsonConstants.Null;
                    return false;
                }
            }

            var success = true;
            decimal decSum = 0;
            foreach (var item in arg0.EnumerateArray())
            {
                if (!item.TryGetDecimal(out var dec))
                {
                    success = false;
                    break;
                }

                decSum += dec;
            }

            if (success)
            {
                element = new DecimalValue(decSum);
                return true;
            }

            double dblSum = 0;
            foreach (var item in arg0.EnumerateArray())
            {
                if (!item.TryGetDouble(out var dbl))
                {
                    element = JsonConstants.Null;
                    return false;
                }

                dblSum += dbl;
            }

            element = new DoubleValue(dblSum);
            return true;
        }

        public override string ToString()
        {
            return "sum";
        }
    }

    internal sealed class ToArrayFunction : BaseFunction
    {
        internal ToArrayFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            if (arg0.Type == JmesPathType.Array)
            {
                element = arg0;
                return true;
            }

            var list = new List<IValue> { arg0 };
            element = new ArrayValue(list);
            return true;
        }

        public override string ToString()
        {
            return "to_array";
        }
    }

    internal sealed class ToNumberFunction : BaseFunction
    {
        internal ToNumberFunction()
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
                case JmesPathType.Number:
                    element = arg0;
                    return true;
                case JmesPathType.String:
                {
                    var s = arg0.GetString();
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                    {
                        element = new DecimalValue(dec);
                        return true;
                    }

                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl))
                    {
                        element = new DoubleValue(dbl);
                        return true;
                    }

                    element = JsonConstants.Null;
                    return false;
                }
                default:
                    element = JsonConstants.Null;
                    return false;
            }
        }

        public override string ToString()
        {
            return "to_number";
        }
    }

    internal sealed class ToStringFunction : BaseFunction
    {
        internal ToStringFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            if (args[0].Type == JmesPathType.Expression)
            {
                element = JsonConstants.Null;
                return false;
            }

            var arg0 = args[0];
            switch (arg0.Type)
            {
                case JmesPathType.String:
                    element = arg0;
                    return true;
                case JmesPathType.Expression:
                    element = JsonConstants.Null;
                    return false;
                default:
                    element = new StringValue(arg0.ToString());
                    return true;
            }
        }

        public override string ToString()
        {
            return "to_string";
        }
    }

    internal sealed class ValuesFunction : BaseFunction
    {
        internal ValuesFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];
            if (arg0.Type != JmesPathType.Object)
            {
                element = JsonConstants.Null;
                return false;
            }

            var list = new List<IValue>();

            foreach (var item in arg0.EnumerateObject())
            {
                list.Add(item.Value);
            }

            element = new ArrayValue(list);
            return true;
        }

        public override string ToString()
        {
            return "values";
        }
    }

    internal sealed class TypeFunction : BaseFunction
    {
        internal TypeFunction()
            : base(1)
        {
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var arg0 = args[0];

            switch (arg0.Type)
            {
                case JmesPathType.Number:
                    element = new StringValue("number");
                    return true;
                case JmesPathType.True:
                case JmesPathType.False:
                    element = new StringValue("boolean");
                    return true;
                case JmesPathType.String:
                    element = new StringValue("string");
                    return true;
                case JmesPathType.Object:
                    element = new StringValue("object");
                    return true;
                case JmesPathType.Array:
                    element = new StringValue("array");
                    return true;
                case JmesPathType.Null:
                    element = new StringValue("null");
                    return true;
                default:
                    element = JsonConstants.Null;
                    return false;
            }
        }

        public override string ToString()
        {
            return "type";
        }
    }

    internal sealed class JsonFunction : BaseFunction
    {
        /// <inheritdoc />
        public JsonFunction()
            : base(1)
        {
        }

        public override string ToString()
        {
            return "powertools_json";
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);
            element = args[0];
            return true;
        }
    }

    internal sealed class Base64Function : BaseFunction
    {
        /// <inheritdoc />
        public Base64Function()
            : base(1)
        {
        }

        public override string ToString()
        {
            return "powertools_base64";
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);
            var base64StringBytes = Convert.FromBase64String(args[0].GetString());
            var doc = JsonDocument.Parse(base64StringBytes);
            element = new JsonElementValue(doc.RootElement);
            return true;
        }
    }

    internal sealed class Base64GzipFunction : BaseFunction
    {
        /// <inheritdoc />
        public Base64GzipFunction()
            : base(1)
        {
        }

        public override string ToString()
        {
            return "powertools_base64_gzip";
        }

        public override bool TryEvaluate(DynamicResources resources, IList<IValue> args, out IValue element)
        {
            Debug.Assert(Arity.HasValue && args.Count == Arity!.Value);

            var compressedBytes = Convert.FromBase64String(args[0].GetString());

            using var compressedStream = new MemoryStream(compressedBytes);
            using var decompressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(decompressedStream);
            }

            var doc = JsonDocument.Parse(Encoding.UTF8.GetString(decompressedStream.ToArray()));
            element = new JsonElementValue(doc.RootElement);

            return true;
        }
    }

    internal sealed class BuiltInFunctions
    {
        internal static BuiltInFunctions Instance { get; } = new();

        private readonly Dictionary<string, IFunction> _functions = new();

        private BuiltInFunctions()
        {
            _functions.Add("abs", new AbsFunction());
            _functions.Add("avg", new AvgFunction());
            _functions.Add("ceil", new CeilFunction());
            _functions.Add("contains", new ContainsFunction());
            _functions.Add("ends_with", new EndsWithFunction());
            _functions.Add("floor", new FloorFunction());
            _functions.Add("join", new JoinFunction());
            _functions.Add("keys", new KeysFunction());
            _functions.Add("length", new LengthFunction());
            _functions.Add("map", new MapFunction());
            _functions.Add("max", new MaxFunction());
            _functions.Add("max_by", new MaxByFunction());
            _functions.Add("merge", new MergeFunction());
            _functions.Add("min", new MinFunction());
            _functions.Add("min_by", new MinByFunction());
            _functions.Add("not_null", new NotNullFunction());
            _functions.Add("reverse", new ReverseFunction());
            _functions.Add("sort", new SortFunction());
            _functions.Add("sort_by", new SortByFunction());
            _functions.Add("starts_with", new StartsWithFunction());
            _functions.Add("sum", new SumFunction());
            _functions.Add("to_array", new ToArrayFunction());
            _functions.Add("to_number", new ToNumberFunction());
            _functions.Add("to_string", new ToStringFunction());
            _functions.Add("type", new TypeFunction());
            _functions.Add("values", new ValuesFunction());
            _functions.Add("powertools_json", new JsonFunction());
            _functions.Add("powertools_base64", new Base64Function());
            _functions.Add("powertools_base64_gzip", new Base64GzipFunction());
        }

        internal bool TryGetFunction(string name, out IFunction func)
        {
            return _functions.TryGetValue(name, out func);
        }
    }
}
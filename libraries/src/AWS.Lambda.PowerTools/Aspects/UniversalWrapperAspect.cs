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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AspectInjector.Broker;

namespace AWS.Lambda.PowerTools.Aspects
{
    /// <summary>
    /// Class UniversalWrapperAspect.
    /// </summary>
    [Aspect(Scope.Global)]
    public class UniversalWrapperAspect
    {
        /// <summary>
        /// Handles the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="type">The type.</param>
        /// <param name="method">The method.</param>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="triggers">The triggers.</param>
        /// <returns>System.Object.</returns>
        [Advice(Kind.Around, Targets = Target.Method)]
        public object Handle(
            [Argument(Source.Instance)] object instance,
            [Argument(Source.Type)] Type type,
            [Argument(Source.Metadata)] MethodBase method,
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Name)] string name,
            [Argument(Source.Arguments)] object[] args,
            [Argument(Source.ReturnType)] Type returnType,
            [Argument(Source.Triggers)] Attribute[] triggers)
        {
            var eventArgs = new AspectEventArgs
            {
                Instance = instance,
                Type = type,
                Method = method,
                Name = name,
                Args = args,
                ReturnType = returnType,
                Triggers = triggers
            };

            var wrappers = triggers.OfType<UniversalWrapperAttribute>().ToArray();
            var handler = GetMethodHandler(method, returnType, wrappers);
            return handler(target, args, eventArgs);
        }

        /// <summary>
        /// Delegate Handler
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="eventArgs">The <see cref="AspectEventArgs"/> instance containing the event data.</param>
        /// <returns>System.Object.</returns>
        private delegate object Handler(Func<object[], object> next, object[] args, AspectEventArgs eventArgs);

        /// <summary>
        /// The delegate cache
        /// </summary>
        private static readonly Dictionary<MethodBase, Handler> _delegateCache = new Dictionary<MethodBase, Handler>();

        /// <summary>
        /// The asynchronous generic handler
        /// </summary>
        private static readonly MethodInfo _asyncGenericHandler =
            typeof(UniversalWrapperAttribute).GetMethod(nameof(UniversalWrapperAttribute.WrapAsync), BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// The synchronize generic handler
        /// </summary>
        private static readonly MethodInfo _syncGenericHandler =
            typeof(UniversalWrapperAttribute).GetMethod(nameof(UniversalWrapperAttribute.WrapSync), BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Creates the method handler.
        /// </summary>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="wrappers">The wrappers.</param>
        /// <returns>Handler.</returns>
        private static Handler CreateMethodHandler(Type returnType, IEnumerable<UniversalWrapperAttribute> wrappers)
        {
            var targetParam = Expression.Parameter(typeof(Func<object[], object>), "orig");
            var eventArgsParam = Expression.Parameter(typeof(AspectEventArgs), "event");

            MethodInfo wrapperMethod;

            if (typeof(Task).IsAssignableFrom(returnType))
            {
                var taskType = returnType.IsConstructedGenericType ? returnType.GenericTypeArguments[0] : Type.GetType("System.Threading.Tasks.VoidTaskResult");
                returnType = typeof(Task<>).MakeGenericType(new[] { taskType });
                wrapperMethod = _asyncGenericHandler.MakeGenericMethod(new[] { taskType });
            }
            else
            {
                if (returnType == typeof(void))
                    returnType = typeof(object);
                wrapperMethod = _syncGenericHandler.MakeGenericMethod(new[] { returnType });
            }

            var converArgs = Expression.Parameter(typeof(object[]), "args");
            var next = Expression.Lambda(Expression.Convert(Expression.Invoke(targetParam, converArgs), returnType), converArgs);

            foreach (var wrapper in wrappers)
            {
                var argsParam = Expression.Parameter(typeof(object[]), "args");
                next = Expression.Lambda(Expression.Call(Expression.Constant(wrapper), wrapperMethod, next, argsParam, eventArgsParam), argsParam);
            }

            var orig_args = Expression.Parameter(typeof(object[]), "orig_args");
            var handler = Expression.Lambda<Handler>(Expression.Convert(Expression.Invoke(next, orig_args), typeof(object)), targetParam, orig_args, eventArgsParam);

            var handlerCompiled = handler.Compile();

            return handlerCompiled;
        }

        /// <summary>
        /// Gets the method handler.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="wrappers">The wrappers.</param>
        /// <returns>Handler.</returns>
        private static Handler GetMethodHandler(MethodBase method, Type returnType, IEnumerable<UniversalWrapperAttribute> wrappers)
        {
            if (!_delegateCache.TryGetValue(method, out var handler))
            {
                lock (method)
                {
                    if (!_delegateCache.TryGetValue(method, out handler))
                    {
                        _delegateCache[method] = handler = CreateMethodHandler(returnType, wrappers);
                    }
                }
            }
            return handler;
        }
    }
}
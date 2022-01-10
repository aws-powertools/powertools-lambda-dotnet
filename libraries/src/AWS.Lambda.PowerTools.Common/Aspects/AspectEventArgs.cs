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
using System.Reflection;

namespace AWS.Lambda.Powertools.Common;

/// <summary>
///     Class AspectEventArgs.
///     Implements the <see cref="System.EventArgs" />
/// </summary>
/// <seealso cref="System.EventArgs" />
public class AspectEventArgs : EventArgs
{
    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public object Instance { get; internal set; }

    /// <summary>
    ///     Gets the type.
    /// </summary>
    /// <value>The type.</value>
    public Type Type { get; internal set; }

    /// <summary>
    ///     Gets the method.
    /// </summary>
    /// <value>The method.</value>
    public MethodBase Method { get; internal set; }

    /// <summary>
    ///     Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; internal set; }

    /// <summary>
    ///     Gets the arguments.
    /// </summary>
    /// <value>The arguments.</value>
    public IReadOnlyList<object> Args { get; internal set; }

    /// <summary>
    ///     Gets the type of the return.
    /// </summary>
    /// <value>The type of the return.</value>
    public Type ReturnType { get; internal set; }

    /// <summary>
    ///     Gets the triggers.
    /// </summary>
    /// <value>The triggers.</value>
    public Attribute[] Triggers { get; internal set; }
}
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

using System.Diagnostics;
using DevLab.JmesPath.Functions;
using Newtonsoft.Json.Linq;

namespace AWS.Lambda.Powertools.Idempotency.Serialization;

/// <summary>
/// Creates JMESPath function <c>powertools_json()</c> to treat the payload as a JSON object rather than a string.
/// </summary>
public class JsonFunction : JmesPathFunction
{
    /// <inheritdoc />
    public JsonFunction()
        : base("powertools_json", 1)
    {
    }

    /// <inheritdoc />
    public override JToken Execute(params JmesPathFunctionArgument[] args)
    {
        Debug.Assert(args.Length == 1);
        Debug.Assert(args[0].IsToken);
        var argument = args[0];
        var token = argument.Token;
        return JToken.Parse(token.ToString());
    }
}
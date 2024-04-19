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
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AWS.Lambda.Powertools.JMESPath.Values;

namespace AWS.Lambda.Powertools.JMESPath.Functions;

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
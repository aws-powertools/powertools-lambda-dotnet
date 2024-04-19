using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

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
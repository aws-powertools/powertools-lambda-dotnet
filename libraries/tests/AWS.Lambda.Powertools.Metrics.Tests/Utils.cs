using System.Collections.Generic;
using System.IO;

namespace AWS.Lambda.Powertools.Metrics.Tests;

public class CustomConsoleWriter : StringWriter
{
    public readonly List<string> OutputValues = new();

    public override void WriteLine(string value)
    {
        OutputValues.Add(value);
        base.WriteLine(value);
    }
}
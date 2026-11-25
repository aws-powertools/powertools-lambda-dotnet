using System.Text.Json;
using Xunit;

// Logging and Metrics write to the process-wide Console, so tests must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PackageSmoke.Tests;

/// <summary>
/// Redirects Console output for the lifetime of the instance, so a test can assert on what Powertools wrote.
/// </summary>
internal sealed class ConsoleCapture : IDisposable
{
    private readonly TextWriter _original = Console.Out;
    private readonly StringWriter _writer = new();

    public ConsoleCapture() => Console.SetOut(_writer);

    public string Output => _writer.ToString();

    /// <summary>Every line of output that is a JSON object.</summary>
    public List<JsonElement> JsonLines() => Output
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(line => line.StartsWith('{'))
        .Select(line => JsonDocument.Parse(line).RootElement.Clone())
        .ToList();

    public void Dispose()
    {
        Console.SetOut(_original);
        _writer.Dispose();
    }
}

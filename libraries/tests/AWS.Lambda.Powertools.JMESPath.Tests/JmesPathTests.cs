using System.Text.Json;
using AWS.Lambda.Powertools.JMESPath.Utilities;
using Xunit.Abstractions;

namespace AWS.Lambda.Powertools.JMESPath.Tests;

public class JmesPathTests
{
    private readonly ITestOutputHelper _output;

    public JmesPathTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Theory]
    [InlineData("test_files/basic.json")]
    [InlineData("test_files/benchmarks.json")]
    [InlineData("test_files/boolean.json")]
    [InlineData("test_files/current.json")]
    [InlineData("test_files/escape.json")]
    [InlineData("test_files/filters.json")]
    [InlineData("test_files/identifiers.json")]
    [InlineData("test_files/indices.json")]
    [InlineData("test_files/literal.json")]
    [InlineData("test_files/multiselect.json")]
    [InlineData("test_files/pipe.json")]
    [InlineData("test_files/slice.json")]
    [InlineData("test_files/unicode.json")]
    [InlineData("test_files/syntax.json")]
    [InlineData("test_files/wildcard.json")]
    [InlineData("test_files/example.json")]
    [InlineData("test_files/functions.json")]
    [InlineData("test_files/test.json")]
    [InlineData("test_files/apigw_event.json")]
    [InlineData("test_files/apigw_event_2.json")]
    public void RunJmesPathTests(string path)
    {
        _output.WriteLine($"Test {path}");
        
        var text = File.ReadAllText(path);
        var jsonOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip
        };
        using var doc = JsonDocument.Parse(text, jsonOptions);
        
        var testsEnumerable = doc.RootElement.EnumerateArray();
        var comparer = JsonElementEqualityComparer.Instance;
        
        foreach (var testGroup in testsEnumerable)
        {
            var given = testGroup.GetProperty("given");
            var testCases = testGroup.GetProperty("cases");
            var testCasesEnumerable = testCases.EnumerateArray();
            foreach (var testCase in testCasesEnumerable)
            {
                var exprElement = testCase.GetProperty("expression");

                try
                {
                    if (testCase.TryGetProperty("error", out var expected))
                    {
                        var msg = expected.GetString();
                        //Debug.WriteLine($"message: {msg}");
                        if (msg != null && (msg.Equals("syntax") || msg.Equals("invalid-arity") || msg.Equals("unknown-function") || msg.Equals("invalid-value")))
                        {
                            Assert.Throws<JmesPathParseException>(() => JsonTransformer.Parse(exprElement.ToString()));
                        }
                        else
                        {
                            var expr = JsonTransformer.Parse(exprElement.ToString());
                            try
                            {
                                var result = expr.Transform(given);
                                using var nullValue = JsonDocument.Parse("null");
                                var success = comparer.Equals(result.RootElement, nullValue.RootElement);
                                Assert.True(success);
                            }
                            catch (InvalidOperationException)
                            { }
                        }
                    }
                    else if (testCase.TryGetProperty("result", out expected))
                    {
                        var expr = JsonTransformer.Parse(exprElement.ToString());
                        var result = expr.Transform(given);
                        var success = comparer.Equals(result.RootElement, expected);
                        if (!success)
                        {
                            _output.WriteLine("File: {0}", path);

                            _output.WriteLine($"Document: {given}");
                            _output.WriteLine($"Path: {exprElement}");
                            _output.WriteLine($"Expected: {JsonSerializer.Serialize(expected)}");
                            _output.WriteLine($"Result: {JsonSerializer.Serialize(result)}");
                        }
                        Assert.True(comparer.Equals(result.RootElement,expected));
                        
                    }
                }
                catch (Exception e)
                {
                    _output.WriteLine("File: {0}", path);
                    _output.WriteLine($"Document: {given}");
                    _output.WriteLine($"Path: {exprElement}");
                    _output.WriteLine("Error: {0}", e.Message);
                    throw;
                }
            }
        }
    }
}
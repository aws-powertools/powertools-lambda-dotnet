// This file is referenced by docs/utilities/metadata.md
// via pymdownx.snippets (mkdocs).


using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Metadata;
using AWS.Lambda.Powertools.Metadata.Exceptions;
using AWS.Lambda.Powertools.Logging;

namespace Docs.MetadataSnippets.GettingStarted.GettingStarted
{
// --8<-- [start:getting_started]
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    public string Handler(object input, ILambdaContext context)
    {
        var azId = LambdaMetadata.AvailabilityZoneId;
        return $"Running in AZ: {azId}";
    }
}
// --8<-- [end:getting_started]
}


namespace Docs.MetadataSnippets.GettingStarted.ErrorHandling
{
// --8<-- [start:error_handling]
using AWS.Lambda.Powertools.Metadata;
using AWS.Lambda.Powertools.Metadata.Exceptions;

internal static class Example { public static void Run() { // --8<-- [start:hidden]
try
{
    var azId = LambdaMetadata.AvailabilityZoneId;
}
catch (LambdaMetadataException ex)
{
    Console.WriteLine($"Failed to get metadata: {ex.Message}");

    if (ex.StatusCode != -1)
        Console.WriteLine($"HTTP Status: {ex.StatusCode}");
}
// --8<-- [end:error_handling]
} }
}


namespace Docs.MetadataSnippets.GettingStarted.RefreshMetadata
{
internal static class Example
{
    public static void Run()
    {
// --8<-- [start:refresh_metadata]
LambdaMetadata.Refresh();
// --8<-- [end:refresh_metadata]
    }
}
}

using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using ILambdaContext = Amazon.Lambda.Core.ILambdaContext;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

// This file is referenced by docs/core/logging.md
// via pymdownx.snippets (mkdocs).


namespace Docs.LoggingSnippets.TimestampFormatting.TimestampFormatting
{
public class Example
{
    public void Run()
    {
// --8<-- [start:timestamp_formatting]
Logger.Configure(logger =>
{
    logger.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
});
// --8<-- [end:timestamp_formatting]
    }
}
}

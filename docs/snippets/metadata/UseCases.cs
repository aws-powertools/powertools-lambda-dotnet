// This file is referenced by docs/utilities/metadata.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Metadata;

// --8<-- [start:multi_az_routing]
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    public async Task<string> Handler(OrderRequest request, ILambdaContext context)
    {
        var endpoint = LambdaMetadata.AvailabilityZoneId switch
        {
            "use1-az1" => "https://service-az1.internal",
            "use1-az2" => "https://service-az2.internal",
            _ => "https://service.internal"
        };

        return await ProcessOrder(request, endpoint);
    }
}
// --8<-- [end:multi_az_routing]

// --8<-- [start:logging_with_metadata]
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metadata;

public class Function
{
    public Function()
    {
        Logger.AppendKey("az_id", LambdaMetadata.AvailabilityZoneId);
    }

    [Logging]
    public string Handler(object input, ILambdaContext context)
    {
        Logger.LogInformation("Processing request");
        return "Success";
    }
}
// --8<-- [end:logging_with_metadata]

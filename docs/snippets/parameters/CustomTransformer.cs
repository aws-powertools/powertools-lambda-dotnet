using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;
using XmlTransformer = Docs.ParametersSnippets.CustomTransformerExamples.XmlTransformerExample.XmlTransformer;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Configuration;
using System.Xml.Serialization;
using Docs.ParametersSnippets.CustomTransformer;

// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).


namespace Docs.ParametersSnippets.CustomTransformerExamples.XmlTransformerExample
{
// --8<-- [start:xml_transformer]
public class XmlTransformer : ITransformer
{
    public T? Transform<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default;

        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(value);
        return (T?)serializer.Deserialize(reader);
    }
}
// --8<-- [end:xml_transformer]
}

namespace Docs.ParametersSnippets.CustomTransformerExamples.UsingXmlTransformer
{
public class Example
{
    private readonly ISsmProvider ssmProvider = ParametersManager.SsmProvider;
    public async Task RunAsync()
    {
// --8<-- [start:using_xml_transformer]
    var value = await ssmProvider
        .WithTransformation(new XmlTransformer())
        .GetAsync<MyObj>("/my/parameter/xml")
        .ConfigureAwait(false);
// --8<-- [end:using_xml_transformer]
    }
}
}

namespace Docs.ParametersSnippets.CustomTransformerExamples.AddingXmlTransformer
{
public class Example
{
    private readonly ISsmProvider ssmProvider = ParametersManager.SsmProvider;
    public async Task RunAsync()
    {
// --8<-- [start:adding_xml_transformer]
    // Get SSM Provider instance
    ISsmProvider ssmProvider = ParametersManager.SsmProvider
        .AddTransformer("XML", new XmlTransformer());

    // Retrieve a single parameter
    var value = await ssmProvider
        .WithTransformation("XML")
        .GetAsync<MyObj>("/my/parameter/xml")
        .ConfigureAwait(false);
// --8<-- [end:adding_xml_transformer]
    }
}
}

namespace Docs.ParametersSnippets.CustomTransformerExamples.FluentApi
{
public class Example
{
    private readonly ISsmProvider ssmProvider = ParametersManager.SsmProvider;
    public async Task RunAsync()
    {
// --8<-- [start:fluent_api]
    ssmProvider
      .DefaultMaxAge(TimeSpan.FromSeconds(10))  // will set 10 seconds as the default cache TTL
      .WithMaxAge(TimeSpan.FromMinutes(1))      // will set the cache TTL for this value at 1 minute
      .WithTransformation(Transformation.Json)  // Will use JSON transfomer to deserializes JSON to an object
      .WithDecryption()                         // enable decryption of the parameter value
      .Get<MyObj>("/my/param");                 // finally get the value
// --8<-- [end:fluent_api]
    }
}
}

namespace Docs.ParametersSnippets.CustomTransformer
{
    public class MyObj { }
}

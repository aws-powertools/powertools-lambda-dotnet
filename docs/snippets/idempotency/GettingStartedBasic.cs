// This file is referenced by docs/utilities/idempotency.md
// via pymdownx.snippets (mkdocs).


using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Idempotency.Persistence;
using AWS.Lambda.Powertools.Docs.Snippets.Idempotency;

namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency.GettingStartedBasic.IdempotentAttributeExample
{
using Idempotency = global::AWS.Lambda.Powertools.Idempotency.Idempotency;
// --8<-- [start:idempotent_attribute]
    public class Function
    {
        public Function()
        {
            Idempotency.Configure(builder => builder.UseDynamoDb("idempotency_table"));
        }
        
        [Idempotent]
        public Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            return Task.FromResult(input.ToUpper());
        }
    }
// --8<-- [end:idempotent_attribute]
}


namespace AWS.Lambda.Powertools.Docs.Snippets.Idempotency.GettingStartedBasic.IdempotentAttributeOnAnotherMethod
{
using Idempotency = global::AWS.Lambda.Powertools.Idempotency.Idempotency;
// --8<-- [start:idempotent_attribute_on_another_method]
    public class Function
    {
        public Function()
        {
            Idempotency.Configure(builder => builder.UseDynamoDb("idempotency_table"));
        }
        
        public Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            MyInternalMethod("hello", "world");
            return Task.FromResult(input.ToUpper());
        }

        [Idempotent]
        private string MyInternalMethod(string argOne, [IdempotencyKey] string argTwo) {
            return "something";
        }
    }
// --8<-- [end:idempotent_attribute_on_another_method]
}

using Amazon.CDK;
using InfraShared;

namespace InfraAot
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            var architecture = app.Node.TryGetContext("architecture")?.ToString();
            if (architecture == null)
            {
                throw new System.ArgumentException(
                    "architecture context is required. Please provide it with --context architecture=arm64|x86_64");
            }

            if (architecture != "arm64" && architecture != "x86_64")
            {
                throw new System.ArgumentException("architecture context must be either arm64 or x86_64");
            }

            var id = "CoreAotStack";
            var idempotencystackAotId = "IdempotencyStack-AOT";
            if (architecture == "arm64")
            {
                id = $"CoreAotStack-{architecture}";
                idempotencystackAotId = $"IdempotencyStack-AOT-{architecture}";
            }

            _ = new CoreAotStack(app, id, new PowertoolsDefaultStackProps
            {
                ArchitectureString = architecture
            });

            _ = new IdempotencyStack(app, idempotencystackAotId,
                new IdempotencyStackProps { IsAot = true, ArchitectureString = architecture, TableName = $"IdempotencyTable-AOT-{architecture}" });

            app.Synth();
        }
    }
}
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
            if (architecture == "arm64")
            {
                id = $"CoreAotStack-{architecture}";
            }

            _ = new CoreAotStack(app, id, new PowertoolsDefaultStackProps
            {
                ArchitectureString = architecture
            });

            _ = new IdempotencyStack(app, $"IdempotencyStack-AOT-{architecture}",
                new PowertoolsDefaultStackProps { IsAot = true, ArchitectureString = architecture });

            app.Synth();
        }
    }
}
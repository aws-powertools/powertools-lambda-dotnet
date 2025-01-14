using Amazon.CDK;

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
                throw new System.ArgumentException("architecture context is required. Please provide it with --context architecture=arm64|x86_64");
            }

            if (architecture != "arm64" && architecture != "x86_64")
            {
                throw new System.ArgumentException("architecture context must be either arm64 or x86_64");
            }

            _ = new CoreAotStack(app, "CoreAotStack", new AotStackProps
            {
                Architecture = architecture
            });
            app.Synth();
        }
    }
}
using Amazon.CDK;
using InfraShared;

namespace Infra
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            _ = new CoreStack(app, "CoreStack", new StackProps { });

            _ = new IdempotencyStack(app, "IdempotencyStack", new PowertoolsDefaultStackProps { });
            
            app.Synth();
        }
    }
}
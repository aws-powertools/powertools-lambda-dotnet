using Amazon.CDK;

namespace Infra
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            _ = new CoreStack(app, "CoreStack", new StackProps { });

            app.Synth();
        }
    }
}
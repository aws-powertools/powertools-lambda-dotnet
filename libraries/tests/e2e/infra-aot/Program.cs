using Amazon.CDK;

namespace InfraAot
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            _ = new CoreAotStack(app, "CoreAotStack", new StackProps { });
            app.Synth();
        }
    }
}

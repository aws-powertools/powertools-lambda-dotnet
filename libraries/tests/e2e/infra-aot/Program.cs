using Amazon.CDK;

namespace InfraAot
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            _ = new CoreAotStack(app, "CoreAotStack", new StackProps { });
            app.Synth();
        }
    }
}

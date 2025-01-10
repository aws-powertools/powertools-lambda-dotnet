using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InfraAot
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new CoreAotStack(app, "CoreAotStack", new StackProps { });
            app.Synth();
        }
    }
}

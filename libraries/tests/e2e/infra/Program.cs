using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E2E
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new CoreStack(app, "CoreStack", new StackProps { });
            new CoreAotStack(app, "CoreAotStack", new StackProps { });
            
            app.Synth();
        }
    }
}

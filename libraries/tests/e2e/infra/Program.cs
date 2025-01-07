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
            new E2EStack(app, "E2EStack", new StackProps { });
            
            app.Synth();
        }
    }
}

﻿using Amazon.CDK;

namespace Infra
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            new CoreStack(app, "CoreStack", new StackProps { });

            app.Synth();
        }
    }
}
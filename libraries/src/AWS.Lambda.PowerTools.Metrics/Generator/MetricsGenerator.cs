/*using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AWS.Lambda.PowerTools.Metrics
{
    [Generator]
    public class MetricsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //context.RegisterForPostInitialization((init) => init.AddSource("Metrics_Attribute_Generated.cs",_metricsAttributeStub));
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the source code to the compilation
            if (context.SyntaxReceiver is not SyntaxReceiver syntaxReceiver)                            
            {                                                                                           
                throw new InvalidOperationException("We were given the wrong syntax receiver.");        
            }                                                                                           
            
            string initialClassStub = $@" // This code was auto-generated
using AWS.Lambda.PowerTools.Metrics;                                                   
                                                                                       
namespace {syntaxReceiver.CompilationNamespace}                                                                   
{{                                                                                     
    public partial class {syntaxReceiver.CompilationClass}                                                    
    {{                  
";                                                                                     
                                                                                       
            string closingClassStub = $@"                                
    }}                                                                                 
}}                                                                                     
";                                                                                     
            
            StringBuilder sb = new StringBuilder(initialClassStub);
            
            
            if (syntaxReceiver.MetricsAttributeExists)
            {
                if (String.IsNullOrWhiteSpace(syntaxReceiver.ServiceName))
                {
                    sb.Append($@"        private static readonly Metrics Metrics = new Metrics();");
                    sb.Append($@"/* {syntaxReceiver.DebugInfo} #1#");
                }
                else
                {
                    sb.Append($@"        private static readonly Metrics Metrics = new Metrics({syntaxReceiver.ServiceNamespace}, {syntaxReceiver.ServiceName});");
                    sb.Append($@"/* {syntaxReceiver.DebugInfo} #1#");
                }    
                
                
            }

            sb.Append(closingClassStub);

            context.AddSource("Metrics_Auto_Generated.cs", sb.ToString());
        }
    }
}*/
using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AWS.Lambda.PowerTools.Metrics.Generator
{
    [Generator]
    public class MetricsGenerator : ISourceGenerator
    {
        private readonly string _metricsAttributeStub = $@"// This code was auto-generated
using System;

namespace AWS.Lambda.PowerTools.Metrics
{{
    [AttributeUsage(AttributeTargets.Method)]
    public class MetricsAttribute : Attribute
    {{
        public MetricsAttribute() {{ }}

        public MetricsAttribute(string serviceNamespace, string serviceName) {{ }}
    }}
}}
";               
        
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("MetricsAttribute", _metricsAttributeStub);    
            
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
                }
                else
                {
                    sb.Append($@"        private static readonly Metrics Metrics = new Metrics({syntaxReceiver.ServiceNamespace}, {syntaxReceiver.ServiceName});");
                }                                                             
            }

            sb.Append(closingClassStub);
            context.AddSource("Metrics_Auto_Generated", sb.ToString());
        }
    }
}
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace AWS.Lambda.PowerTools.Metrics
{
    [Generator]
    public class MetricsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            string source = $@"
using AWS.Lambda.PowerTools.Metrics;

namespace HelloWorld
{{
    public partial class Function
    {{
        private static readonly Metrics _metricsLogger = new Metrics(""dotnet-lambdapowertools"", ""lambda-example"");
    }}
}}
";
                context.AddSource("generatedSource", source);


                /*var syntaxTrees = context.Compilation.SyntaxTrees;
    
                foreach (var syntaxTree in syntaxTrees)
                {
                    var captureMetricsDeclarations = syntaxTree
                        .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                        .Where(x => x.AttributeLists.Any(xx => xx.ToString().StartsWith("[Metrics"))).ToList();
    
                    foreach (var captureMetricsDeclaration in captureMetricsDeclarations)
                    {
                        var usingDirectives = syntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>();
                        var usingDirectivesAsText = string.Join("\r\n", usingDirectives);
                        var sourceBuilder = new StringBuilder(usingDirectivesAsText);
    
                        
                        var className = captureMetricsDeclaration.Identifier.ToString();
                        var genClassName = $"{className}_powertools_metrics";
                        
                        var splitMethod
                    }
                }*/
        }
    }
}
/*using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AWS.Lambda.PowerTools.Metrics
{
    public class SyntaxReceiver : ISyntaxReceiver
    {
        private readonly string MetricsAttributeName = "Metrics"; 
        internal bool MetricsAttributeExists = false;
        internal string ServiceNamespace = "";
        internal string ServiceName = "";
        internal string CompilationNamespace = "";
        internal string CompilationClass = "";
        
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is MethodDeclarationSyntax
            {
                AttributeLists:
                {
                    Count: >= 0
                } attributes
            } methodSyntax && attributes.Any(list=> list.Attributes.Any(attr => attr.Name.ToString() == MetricsAttributeName)) )
            {
                // Get class namespace name
                var firstMember = methodSyntax.SyntaxTree.GetCompilationUnitRoot().Members.First();
                var namespaceMember = (NamespaceDeclarationSyntax) firstMember;
                CompilationNamespace = namespaceMember.Name.ToString();
                var metricsAttribute = attributes.SelectMany(x => x.Attributes).FirstOrDefault(attr => attr.Name.ToString() == MetricsAttributeName);

                // Get class name
                var programDeclaration = (ClassDeclarationSyntax)namespaceMember.Members[0];
                CompilationClass = programDeclaration.Identifier.ToString();

                MetricsAttributeExists = metricsAttribute != null;

                if (metricsAttribute?.ArgumentList?.Arguments.Count > 0)
                {
                    ServiceNamespace = metricsAttribute.ArgumentList.Arguments[0]?.GetText().Container.CurrentText
                        .ToString();
                    ServiceName = metricsAttribute.ArgumentList.Arguments[1]?.GetText().Container.CurrentText
                        .ToString();
                }     
            }
        }
    }
}*/
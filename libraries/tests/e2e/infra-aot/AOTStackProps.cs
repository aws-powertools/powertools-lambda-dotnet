using Amazon.CDK;

namespace InfraAot;

public class AotStackProps : StackProps
{
    public string Architecture { get; set; }
}
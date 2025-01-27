using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;

namespace InfraShared;

public class FunctionConstructProps : PowertoolsDefaultStackProps
{
    public required Architecture Architecture { get; set; }
    public required Runtime Runtime { get; set; }
    public required string Name { get; set; }
    public required string Handler { get; set; }
    public required string SourcePath { get; set; }
    public required string DistPath { get; set; }
}

public class PowertoolsDefaultStackProps : StackProps
{
    public bool IsAot { get; set; } = false;
    public string? ArchitectureString { get; set; }
    public Dictionary<string,string>? Environment { get; set; }
}
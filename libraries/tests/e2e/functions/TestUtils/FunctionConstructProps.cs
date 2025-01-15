using Amazon.CDK.AWS.Lambda;

namespace TestUtils;

public class FunctionConstructProps
{
    public required Architecture Architecture { get; set; }
    public required Runtime Runtime { get; set; }
    public required string Name { get; set; }
    public required string Handler { get; set; }
    public required string SourcePath { get; set; }
    public required string DistPath { get; set; }
}
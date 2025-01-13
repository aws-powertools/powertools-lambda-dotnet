using Amazon.CDK.AWS.Lambda;

namespace TestUtils;

public class FunctionConstructProps
{
    public Architecture Architecture;
    public Runtime Runtime;
    public string Name;
    public string Handler;
    public string SourcePath;
    public string DistPath;
}
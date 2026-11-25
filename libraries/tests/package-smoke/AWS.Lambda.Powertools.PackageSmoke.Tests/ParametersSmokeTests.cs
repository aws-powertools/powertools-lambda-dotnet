using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SecretsManager;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;
using AWS.Lambda.Powertools.Parameters.Transform;
using NSubstitute;
using Xunit;

namespace PackageSmoke.Tests;

public class AppSettings
{
    public string Greeting { get; set; }
    public int Retries { get; set; }
}

public class ParametersFunction
{
    private readonly ISsmProvider _ssm;
    private readonly ISecretsProvider _secrets;

    public ParametersFunction(IAmazonSimpleSystemsManagement ssmClient, IAmazonSecretsManager secretsClient)
    {
        _ssm = ParametersManager.CreateSsmProvider().UseClient(ssmClient);
        _secrets = ParametersManager.CreateSecretsProvider().UseClient(secretsClient);
    }

    public Task<string> GetParameter(string name) => _ssm.GetAsync(name);

    public Task<AppSettings> GetSettings(string name) =>
        _ssm.WithTransformation(Transformation.Json).GetAsync<AppSettings>(name);

    public Task<string> GetSecret(string name) => _secrets.GetAsync(name);
}

public class ParametersSmokeTests
{
    private readonly IAmazonSimpleSystemsManagement _ssmClient = Substitute.For<IAmazonSimpleSystemsManagement>();
    private readonly IAmazonSecretsManager _secretsClient = Substitute.For<IAmazonSecretsManager>();

    private void GivenParameter(string name, string value) =>
        _ssmClient.GetParameterAsync(Arg.Is<GetParameterRequest>(r => r.Name == name), Arg.Any<CancellationToken>())
            .Returns(new GetParameterResponse { Parameter = new Parameter { Name = name, Value = value } });

    [Fact]
    public async Task Ssm_Parameter_Is_Fetched_Once_And_Then_Cached()
    {
        var name = $"/smoke/{Guid.NewGuid():N}";
        GivenParameter(name, "parameter-value");
        var function = new ParametersFunction(_ssmClient, _secretsClient);

        var first = await function.GetParameter(name);
        var second = await function.GetParameter(name);

        Assert.Equal("parameter-value", first);
        Assert.Equal("parameter-value", second);
        await _ssmClient.Received(1).GetParameterAsync(Arg.Any<GetParameterRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ssm_Json_Parameter_Is_Transformed()
    {
        var name = $"/smoke/{Guid.NewGuid():N}";
        GivenParameter(name, "{\"Greeting\":\"hello\",\"Retries\":3}");

        var settings = await new ParametersFunction(_ssmClient, _secretsClient).GetSettings(name);

        Assert.Equal("hello", settings.Greeting);
        Assert.Equal(3, settings.Retries);
    }

    [Fact]
    public async Task Secret_Is_Fetched_From_Secrets_Manager()
    {
        var name = $"smoke/{Guid.NewGuid():N}";
        _secretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == name), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse { Name = name, SecretString = "secret-value" });

        var secret = await new ParametersFunction(_ssmClient, _secretsClient).GetSecret(name);

        Assert.Equal("secret-value", secret);
    }
}

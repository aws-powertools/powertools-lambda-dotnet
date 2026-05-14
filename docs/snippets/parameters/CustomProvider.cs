// This file is referenced by docs/utilities/parameters.md
// via pymdownx.snippets (mkdocs).

namespace AWS.Lambda.Powertools.Docs.Snippets.Parameters;

// --8<-- [start:s3_provider]
public class S3Provider : ParameterProvider
{

    private string _bucket;
    private readonly IAmazonS3 _client;

    public S3Provider()
    {
        _client = new AmazonS3Client();
    }

    public S3Provider(IAmazonS3 client)
    {
        _client = client;
    }

    public S3Provider WithBucket(string bucket)
    {
        _bucket = bucket;
        return this;
    }

    protected override async Task<string?> GetAsync(string key, ParameterProviderConfiguration? config)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        if (string.IsNullOrEmpty(_bucket))
            throw new ArgumentException("A bucket must be specified, using withBucket() method");

        var request = new GetObjectRequest
        {
            Key = key,
            BucketName = _bucket
        };

        using var response = await _client.GetObjectAsync(request);
        await using var responseStream = response.ResponseStream;
        using var reader = new StreamReader(responseStream);
        return await reader.ReadToEndAsync();
    }

     protected override async Task<IDictionary<string, string?>> GetMultipleAsync(string path, ParameterProviderConfiguration? config)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (string.IsNullOrEmpty(_bucket))
            throw new ArgumentException("A bucket must be specified, using withBucket() method");

        var request = new ListObjectsV2Request
        {
            Prefix = path,
            BucketName = _bucket
        };
        var response = await _client.ListObjectsV2Async(request);

        var result = new Dictionary<string, string?>();
        foreach (var s3Object in response.S3Objects)
        {
            var value = await GetAsync(s3Object.Key);
            result.Add(s3Object.Key, value);
        }

        return result;
    }
}
// --8<-- [end:s3_provider]

// --8<-- [start:using_custom_provider]
    var provider = new S3Provider();

    var value = await provider
        .WithBucket("myBucket")
        .GetAsync("myKey")
        .ConfigureAwait(false);
// --8<-- [end:using_custom_provider]

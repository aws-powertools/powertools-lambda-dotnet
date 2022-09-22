namespace AWS.Lambda.Powertools.Parameters.Provider;

public interface IProviderBase
{
    string? Get(string key);
    Task<string?> GetAsync(string key);

    T? Get<T>(string key) where T : class;

    Task<T?> GetAsync<T>(string key) where T : class;

    IDictionary<string, string> GetMultiple(string path);

    Task<IDictionary<string, string>> GetMultipleAsync(string path);
}
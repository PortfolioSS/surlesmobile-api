using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace SurlesMobile.Api.Services;

public class AwsSecretsService : ISecretsService
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly ILogger<AwsSecretsService> _logger;
    private readonly Dictionary<string, string> _cache = new();

    public AwsSecretsService(IAmazonSecretsManager secretsManager, ILogger<AwsSecretsService> logger)
    {
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        if (_cache.TryGetValue(secretName, out var cachedValue))
        {
            return cachedValue;
        }

        try
        {
            var request = new GetSecretValueRequest { SecretId = secretName };
            var response = await _secretsManager.GetSecretValueAsync(request);
            
            var secretValue = response.SecretString;
            _cache[secretName] = secretValue;
            
            _logger.LogDebug("Successfully retrieved secret: {SecretName}", secretName);
            return secretValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret: {SecretName}", secretName);
            throw;
        }
    }

    public async Task<T> GetSecretAsync<T>(string secretName) where T : class
    {
        var secretValue = await GetSecretAsync(secretName);
        
        try
        {
            var result = JsonSerializer.Deserialize<T>(secretValue);
            return result ?? throw new InvalidOperationException($"Failed to deserialize secret {secretName} to type {typeof(T).Name}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize secret {SecretName} to type {Type}", secretName, typeof(T).Name);
            throw;
        }
    }
}
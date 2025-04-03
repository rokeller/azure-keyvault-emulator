using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using AzureKeyVaultEmulator.Secrets.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AzureKeyVaultEmulator.Secrets.Services;

internal sealed class KeyVaultSecretService : IKeyVaultSecretService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ConcurrentDictionary<string, SecretResponse> secrets = new();
    private readonly Store<SecretResponse> store;

    public KeyVaultSecretService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<StoreOptions> storeOptions)
    {
        this.httpContextAccessor = httpContextAccessor;

        string secretsStorageDir = Path.Combine(storeOptions.Value.BaseDir, "secrets");
        store = new(new(secretsStorageDir));
        LoadFromStore();
    }

    public IEnumerable<SecretResponse> Get(string name)
    {
        string cachePrefix = GetCacheIdPrefix(name);
        foreach (KeyValuePair<string, SecretResponse> cacheItem in secrets)
        {
            if (cacheItem.Key.StartsWith(cachePrefix))
            {
                yield return cacheItem.Value;
            }
        }
    }

    public SecretResponse? Get(string name, string version)
    {
        if (!secrets.TryGetValue(GetCacheId(name, version), out var found))
        {
            return null;
        }

        return found;
    }

    public SecretResponse SetSecret(string name, SetSecretModel secret)
    {
        var version = Guid.NewGuid().ToString("N");
        var secretUrl = new UriBuilder
        {
            Scheme = httpContextAccessor.HttpContext.Request.Scheme,
            Host = httpContextAccessor.HttpContext.Request.Host.Host,
            Port = httpContextAccessor.HttpContext.Request.Host.Port ?? -1,
            Path = $"secrets/{name}/{version}"
        };

        var response = new SecretResponse
        {
            Id = secretUrl.Uri,
            Value = secret.Value,
            Attributes = secret.Attributes,
            ContentType = secret.ContentType,
            Tags = secret.Tags
        };

        string cacheKey = GetCacheId(name, version);
        secrets.AddOrUpdate(cacheKey, response, (_, _) => response);
        store.StoreObject(cacheKey, response);

        return response;
    }

    public SecretResponse UpdateSecret(string name, string version, SecretResponse newSecret)
    {
        string cacheKey = GetCacheId(name, version);
        if (!secrets.TryGetValue(GetCacheId(name, version), out var curSecret))
        {
            throw new InvalidOperationException("The secret version does not exist.");
        }

        var secret = newSecret with
        {
            Id = curSecret.Id
        };

        secrets.AddOrUpdate(cacheKey, secret, (_, _) => secret);
        store.StoreObject(cacheKey, secret);

        return secret;
    }

    private static string GetCacheId(string name, string version)
    {
        return $"{name}~{version}";
    }

    private static string GetCacheIdPrefix(string name)
    {
        return $"{name}~";
    }

    private void LoadFromStore()
    {
        foreach (KeyValuePair<string, SecretResponse> secret in store.ReadObjects())
        {
            // Skip old secrets from store that do not have a version in the name.
            if (secret.Key.IndexOf('~') < 0)
            {
                continue;
            }

            secrets.TryAdd(secret.Key, secret.Value);
        }
    }
}

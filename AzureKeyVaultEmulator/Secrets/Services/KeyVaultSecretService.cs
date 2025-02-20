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

    public SecretResponse Get(string name)
    {
        secrets.TryGetValue(GetCacheId(name), out var found);

        return found;
    }

    public SecretResponse Get(string name, string version)
    {
        secrets.TryGetValue(GetCacheId(name, version), out var found);

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
            Attributes = secret.SecretAttributes,
            Tags = secret.Tags
        };

        string secretKey = GetCacheId(name);
        string versionKey = GetCacheId(name, version);
        secrets.AddOrUpdate(secretKey, response, (_, _) => response);
        secrets.TryAdd(versionKey, response);

        store.StoreObject(secretKey, response);
        store.StoreObject(versionKey, response);

        return response;
    }

    private static string GetCacheId(string name, string version = null)
    {
        return null == version ? name : $"{name}~{version}";
    }

    private void LoadFromStore()
    {
        foreach (KeyValuePair<string, SecretResponse> secret in store.ReadObjects())
        {
            secrets.TryAdd(secret.Key, secret.Value);
        }
    }
}

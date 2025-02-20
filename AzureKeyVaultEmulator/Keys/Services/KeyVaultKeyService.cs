using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AzureKeyVaultEmulator.Keys.Factories;
using AzureKeyVaultEmulator.Keys.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AzureKeyVaultEmulator.Keys.Services;

internal sealed class KeyVaultKeyService : IKeyVaultKeyService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ConcurrentDictionary<string, KeyResponse> keys = new();
    private readonly JsonSerializerOptions readOptions =
        JsonWebKeyModelDeserializer.AddConverter(new());
    private readonly Store<KeyResponse> store;

    public KeyVaultKeyService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<StoreOptions> storeOptions)
    {
        this.httpContextAccessor = httpContextAccessor;

        string keysStorageDir = Path.Combine(storeOptions.Value.BaseDir, "keys");
        store = new(new(keysStorageDir), null, readOptions);
        LoadFromStore();
    }

    public KeyResponse Get(string name)
    {
        keys.TryGetValue(GetCacheId(name), out var found);

        return found;
    }

    public KeyResponse Get(string name, string version)
    {
        keys.TryGetValue(GetCacheId(name, version), out var found);

        return found;
    }

    public KeyResponse CreateKey(string name, CreateKeyModel key)
    {
        JsonWebKeyModel jsonWebKeyModel;
        switch (key.KeyType)
        {
            case "RSA":
                var rsaKey = RsaKeyFactory.CreateRsaKey(key.KeySize);
                jsonWebKeyModel = new JsonWebKeyModel(rsaKey);
                break;

            default:
                throw new NotImplementedException($"KeyType {key.KeyType} is not supported");
        }

        var version = Guid.NewGuid().ToString("N");
        var keyUrl = new UriBuilder
        {
            Scheme = httpContextAccessor.HttpContext.Request.Scheme,
            Host = httpContextAccessor.HttpContext.Request.Host.Host,
            Port = httpContextAccessor.HttpContext.Request.Host.Port ?? -1,
            Path = $"keys/{name}/{version}"
        };

        jsonWebKeyModel.KeyName = name;
        jsonWebKeyModel.KeyVersion = version;
        jsonWebKeyModel.KeyIdentifier = keyUrl.Uri.ToString();
        jsonWebKeyModel.KeyOperations = key.KeyOperations;

        var response = new KeyResponse
        {
            Key = jsonWebKeyModel,
            Attributes = key.KeyAttributes,
            Tags = key.Tags
        };

        string keyKey = GetCacheId(name);
        string versionKey = GetCacheId(name, version);
        keys.AddOrUpdate(keyKey, response, (_, _) => response);
        keys.TryAdd(GetCacheId(name, version), response);

        store.StoreObject(keyKey, response);
        store.StoreObject(versionKey, response);

        return response;
    }

    public KeyOperationResult Encrypt(string name, string version, KeyOperationParameters keyOperationParameters)
    {
        var foundKey = Get(name, version);
        if (null == foundKey)
        {
            return null;
        }

        var encrypted = WebEncoders.Base64UrlEncode(foundKey.Key.Encrypt(keyOperationParameters));

        return new KeyOperationResult
        {
            KeyIdentifier = foundKey.Key.KeyIdentifier,
            Data = encrypted
        };
    }

    public KeyOperationResult Decrypt(string name, string version, KeyOperationParameters keyOperationParameters)
    {
        var foundKey = Get(name, version);
        if (null == foundKey)
        {
            return null;
        }

        var decrypted = foundKey.Key.Decrypt(keyOperationParameters);

        return new KeyOperationResult
        {
            KeyIdentifier = foundKey.Key.KeyIdentifier,
            Data = decrypted
        };
    }

    public KeyOperationResult WrapKey(string name, string version, KeyOperationParameters request)
    {
        KeyResponse key = Get(name, version);
        if (null == key)
        {
            return null;
        }
        var encrypted = key.Key.Encrypt(request);

        return new KeyOperationResult()
        {
            KeyIdentifier = key.Key.KeyIdentifier,
            Data = WebEncoders.Base64UrlEncode(encrypted),
        };
    }

    public KeyOperationResult UnwrapKey(string name, string version, KeyOperationParameters request)
    {
        KeyResponse key = Get(name, version);
        if (key == null)
        {
            return null;
        }
        var decrypted = key.Key.Decrypt(request);

        return new KeyOperationResult()
        {
            KeyIdentifier = key.Key.KeyIdentifier,
            Data = decrypted,
        };
    }

    private static string GetCacheId(string name, string version = null)
    {
        return null == version ? name : $"{name}~{version}";
    }

    private static (string name, string version) ExtractNameFromCacheId(string cacheId)
    {
        int lastTilde = cacheId.LastIndexOf('~');
        if (lastTilde < 0)
        {
            return (cacheId, null);
        }

        return (cacheId.Substring(0, lastTilde), cacheId.Substring(lastTilde + 1));
    }

    private void LoadFromStore()
    {
        foreach (KeyValuePair<string, KeyResponse> key in store.ReadObjects())
        {
            (string name, string version) = ExtractNameFromCacheId(key.Key);
            key.Value.Key.KeyName = name;
            key.Value.Key.KeyVersion = version;
            keys.TryAdd(key.Key, key.Value);
        }
    }
}

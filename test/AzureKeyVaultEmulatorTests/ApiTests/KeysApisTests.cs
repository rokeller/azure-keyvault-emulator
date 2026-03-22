using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureKeyVaultEmulator.ApiTests;

public abstract partial class KeysApisTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new();

    private readonly KeyClient client;
    private readonly HttpClient httpClient;
    private readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
    private readonly KeyClientOptions.ServiceVersion serviceVersion;

    public KeysApisTests(KeyClientOptions.ServiceVersion serviceVersion)
    {
        factory.ClientOptions.BaseAddress = new("https://localhost.vault.azure.net/");
        httpClient = factory.CreateClient();
        KeyClientOptions options = new(serviceVersion)
        {
            Transport = new HttpClientTransport(httpClient),
            RetryPolicy = new RetryPolicy(maxRetries: 0),
        };

        client = new(factory.ClientOptions.BaseAddress, new LocalTokenCredential(), options);
        this.serviceVersion = serviceVersion;
    }

    public void Dispose()
    {
        ((IDisposable)factory).Dispose();
    }

    private CryptographyClient CreateCryptoClient(KeyVaultKey key)
    {
        CryptographyClientOptions options = new((CryptographyClientOptions.ServiceVersion)serviceVersion)
        {
            Transport = new HttpClientTransport(factory.CreateClient()),
            RetryPolicy = new RetryPolicy(maxRetries: 0),
        };
        CryptographyClient cryptoClient = new(key.Id, new LocalTokenCredential(), options);

        return cryptoClient;
    }

    private CryptographyClient CreateCryptoClient(Uri keyId)
    {
        CryptographyClientOptions options = new((CryptographyClientOptions.ServiceVersion)serviceVersion)
        {
            Transport = new HttpClientTransport(factory.CreateClient()),
            RetryPolicy = new RetryPolicy(maxRetries: 0),
        };
        CryptographyClient cryptoClient = new(keyId, new LocalTokenCredential(), options);

        return cryptoClient;
    }

    private async Task<KeyVaultKey> CreateKeyAsync(string name)
    {
        return (await client.CreateKeyAsync(name, KeyType.Oct, new()
        {
            Enabled = true,
        })).Value;
    }

    private async Task<KeyVaultKey> CreateRsaKeyAsync(string name)
    {
        return (await client.CreateKeyAsync(name, KeyType.Rsa, new()
        {
            Enabled = true,
        })).Value;
    }

    private async Task<KeyVaultKey> CreateEcKeyAsync(string name)
    {
        return (await client.CreateKeyAsync(name, KeyType.Ec, new()
        {
            Enabled = true,
        })).Value;
    }
}

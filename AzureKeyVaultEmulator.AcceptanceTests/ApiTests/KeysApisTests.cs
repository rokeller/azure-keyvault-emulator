using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

public sealed partial class KeysApisTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new();

    private readonly KeyClient client;
    private readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

    public KeysApisTests()
    {
        factory.ClientOptions.BaseAddress = new("https://localhost.vault.azure.net/");
        KeyClientOptions options = new()
        {
            Transport = new HttpClientTransport(factory.CreateClient()),
            RetryPolicy = new RetryPolicy(maxRetries: 0),
        };

        client = new(factory.ClientOptions.BaseAddress, new LocalTokenCredential(), options);
    }

    public void Dispose()
    {
        ((IDisposable)factory).Dispose();
    }

    private CryptographyClient CreateCryptoClient(KeyVaultKey key)
    {
        CryptographyClientOptions options = new()
        {
            Transport = new HttpClientTransport(factory.CreateClient()),
            RetryPolicy = new RetryPolicy(maxRetries: 0),
        };
        CryptographyClient cryptoClient = new(key.Id, new LocalTokenCredential(), options);

        return cryptoClient;
    }

    private CryptographyClient CreateCryptoClient(Uri keyId)
    {
        CryptographyClientOptions options = new()
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
}

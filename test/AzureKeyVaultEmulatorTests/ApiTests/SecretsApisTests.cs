using System;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AzureKeyVaultEmulator.ApiTests;

public sealed partial class SecretsApisTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new();

    private readonly SecretClient client;

    public SecretsApisTests()
    {
        factory.ClientOptions.BaseAddress = new("https://localhost.vault.azure.net/");
        SecretClientOptions options = new()
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

    private async Task<KeyVaultSecret> CreateSecretAsync(string name)
    {
        return (await client.SetSecretAsync(new KeyVaultSecret(name, Guid.NewGuid().ToString())
        {
            Properties =
            {
                Enabled = true,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
                NotBefore = DateTimeOffset.UtcNow,
                Tags =
                {
                    {"environment", "local"},
                    {"testing", "true"},
                },
            },
        })).Value;
    }
}

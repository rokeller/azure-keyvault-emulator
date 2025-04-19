using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests;

public sealed class SecretsApisTests : IDisposable
{
    private readonly WebApplicationFactory<Emulator> factory = new();

    private readonly SecretClient client;

    public SecretsApisTests()
    {
        factory.ClientOptions.BaseAddress = new("https://localhost.vault.azure.net/");
        SecretClientOptions options = new()
        {
            Transport = new HttpClientTransport(factory.CreateClient()),
        };

        client = new(factory.ClientOptions.BaseAddress, new LocalTokenCredential(), options);
    }

    public void Dispose()
    {
        ((IDisposable)factory).Dispose();
    }

    [Fact]
    public async Task SetSecretCreatesNewSecret()
    {
        KeyVaultSecret secret = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
        {
            Properties =
            {
                Enabled = true,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
                NotBefore = DateTimeOffset.UtcNow,
                Tags =
                {
                    { "environment", "local" },
                    { "testing", "true" },
                },
            },
        };

        Response<KeyVaultSecret> result = await client.SetSecretAsync(secret);
        Assert.NotNull(result);

        KeyVaultSecret createdSecret = result.Value;
        Assert.NotNull(createdSecret);

        Assert.NotNull(createdSecret.Id);
        Assert.Equal(secret.Value, createdSecret.Value);
        Assert.Equal(secret.Properties.Enabled, createdSecret.Properties.Enabled);
        Assert.Equal(secret.Properties.ExpiresOn.Value.ToUnixTimeSeconds(),
            createdSecret.Properties.ExpiresOn.GetValueOrDefault().ToUnixTimeSeconds());
        Assert.Equal(secret.Properties.NotBefore.Value.ToUnixTimeSeconds(),
            createdSecret.Properties.NotBefore.GetValueOrDefault().ToUnixTimeSeconds());
        Assert.NotNull(createdSecret.Properties.Version);
        Assert.Equal("local", createdSecret.Properties.Tags["environment"]);
        Assert.Equal("true", createdSecret.Properties.Tags["testing"]);
    }

    [Fact]
    public async Task GetSecretByNameOnlyGetsLatestVersion()
    {
        string expectedName = Guid.NewGuid().ToString();

        Response<KeyVaultSecret> secret1 = await CreateSecret(expectedName);
        Response<KeyVaultSecret> actualLatest = await client.GetSecretAsync(expectedName);
        Assert.Equal(secret1.Value.Id, actualLatest.Value.Id);
        Assert.Equal(secret1.Value.Value, actualLatest.Value.Value);
        Assert.Equal(secret1.Value.Properties.Enabled, actualLatest.Value.Properties.Enabled);
        Assert.Equal(secret1.Value.Properties.NotBefore, actualLatest.Value.Properties.NotBefore);
        Assert.Equal(secret1.Value.Properties.ExpiresOn, actualLatest.Value.Properties.ExpiresOn);
        Assert.Equal(secret1.Value.Properties.Version, actualLatest.Value.Properties.Version);

        Response<KeyVaultSecret> secret2 = await CreateSecret(expectedName);
        actualLatest = await client.GetSecretAsync(expectedName);
        Assert.Equal(secret2.Value.Id, actualLatest.Value.Id);
        Assert.Equal(secret2.Value.Value, actualLatest.Value.Value);
        Assert.Equal(secret2.Value.Properties.Enabled, actualLatest.Value.Properties.Enabled);
        Assert.Equal(secret2.Value.Properties.NotBefore, actualLatest.Value.Properties.NotBefore);
        Assert.Equal(secret2.Value.Properties.ExpiresOn, actualLatest.Value.Properties.ExpiresOn);
        Assert.Equal(secret2.Value.Properties.Version, actualLatest.Value.Properties.Version);
    }

    [Fact]
    public async Task GetSecretByVersionWorks()
    {
        string expectedName = Guid.NewGuid().ToString();
        Response<KeyVaultSecret> expectedSecret = await CreateSecret(expectedName);

        Response<KeyVaultSecret> actualLatestSecret = await client
            .GetSecretAsync(expectedName, expectedSecret.Value.Properties.Version);
        Assert.Equal(expectedSecret.Value.Id, actualLatestSecret.Value.Id);
        Assert.Equal(expectedSecret.Value.Properties.Version, actualLatestSecret.Value.Properties.Version);
    }

    [Fact]
    public async Task GetSecretByOlderVersionWorks()
    {
        string expectedName = Guid.NewGuid().ToString();
        Response<KeyVaultSecret> expected = await CreateSecret(expectedName);

        await CreateSecret(expectedName);

        Response<KeyVaultSecret> actualLatest = await client
            .GetSecretAsync(expectedName, expected.Value.Properties.Version);
        Assert.Equal(expected.Value.Id, actualLatest.Value.Id);
        Assert.Equal(expected.Value.Properties.Version, actualLatest.Value.Properties.Version);
    }

    [Fact]
    public async Task GetSecretForInexistentSecretResultsIn404()
    {
        string name = Guid.NewGuid().ToString();

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecretAsync(name));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task GetSecretByVersionForInexistentVersionResultsIn404()
    {
        string name = Guid.NewGuid().ToString();
        string version = Guid.NewGuid().ToString();

        await CreateSecret(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecretAsync(name, version));
        Assert.Equal(404, ex.Status);
    }

    private async Task<Response<KeyVaultSecret>> CreateSecret(string name)
    {
        return await client.SetSecretAsync(new KeyVaultSecret(name, Guid.NewGuid().ToString())
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
        });
    }
}

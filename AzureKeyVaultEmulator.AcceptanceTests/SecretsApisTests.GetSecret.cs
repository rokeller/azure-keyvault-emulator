using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task GetSecretByNameOnlyGetsLatestVersion()
    {
        string expectedName = Guid.NewGuid().ToString();

        Response<KeyVaultSecret> secret1 = await CreateSecretAsync(expectedName);
        Response<KeyVaultSecret> actualLatest = await client.GetSecretAsync(expectedName);
        Assert.Equal(secret1.Value.Id, actualLatest.Value.Id);
        Assert.Equal(secret1.Value.Value, actualLatest.Value.Value);
        Assert.Equal(secret1.Value.Properties.Enabled, actualLatest.Value.Properties.Enabled);
        Assert.Equal(secret1.Value.Properties.NotBefore, actualLatest.Value.Properties.NotBefore);
        Assert.Equal(secret1.Value.Properties.ExpiresOn, actualLatest.Value.Properties.ExpiresOn);
        Assert.Equal(secret1.Value.Properties.Version, actualLatest.Value.Properties.Version);

        Response<KeyVaultSecret> secret2 = await CreateSecretAsync(expectedName);
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
        Response<KeyVaultSecret> expectedSecret = await CreateSecretAsync(expectedName);

        Response<KeyVaultSecret> actualLatestSecret = await client
            .GetSecretAsync(expectedName, expectedSecret.Value.Properties.Version);
        Assert.Equal(expectedSecret.Value.Id, actualLatestSecret.Value.Id);
        Assert.Equal(expectedSecret.Value.Properties.Version, actualLatestSecret.Value.Properties.Version);
    }

    [Fact]
    public async Task GetSecretByOlderVersionWorks()
    {
        string expectedName = Guid.NewGuid().ToString();
        Response<KeyVaultSecret> expected = await CreateSecretAsync(expectedName);

        await CreateSecretAsync(expectedName);

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

        await CreateSecretAsync(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecretAsync(name, version));
        Assert.Equal(404, ex.Status);
    }
}

using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task GetKeyByNameOnlyGetsLatestVersion()
    {
        string expectedName = Guid.NewGuid().ToString();

        KeyVaultKey key1 = await CreateKeyAsync(expectedName);
        KeyVaultKey actualLatest = await client.GetKeyAsync(expectedName);
        Assert.Equal(key1.Id, actualLatest.Id);
        Assert.Equal(key1.Properties.Enabled, actualLatest.Properties.Enabled);
        Assert.Equal(key1.Properties.NotBefore, actualLatest.Properties.NotBefore);
        Assert.Equal(key1.Properties.ExpiresOn, actualLatest.Properties.ExpiresOn);
        Assert.Equal(key1.Properties.Version, actualLatest.Properties.Version);

        KeyVaultKey key2 = await CreateKeyAsync(expectedName);
        actualLatest = await client.GetKeyAsync(expectedName);
        Assert.Equal(key2.Id, actualLatest.Id);
        Assert.Equal(key2.Properties.Enabled, actualLatest.Properties.Enabled);
        Assert.Equal(key2.Properties.NotBefore, actualLatest.Properties.NotBefore);
        Assert.Equal(key2.Properties.ExpiresOn, actualLatest.Properties.ExpiresOn);
        Assert.Equal(key2.Properties.Version, actualLatest.Properties.Version);
    }

    [Fact]
    public async Task GetKeyByVersionWorks()
    {
        string expectedName = Guid.NewGuid().ToString();
        KeyVaultKey expectedKey = await CreateKeyAsync(expectedName);

        Response<KeyVaultKey> actualLatestKey = await client
            .GetKeyAsync(expectedName, expectedKey.Properties.Version);
        Assert.Equal(expectedKey.Id, actualLatestKey.Value.Id);
        Assert.Equal(expectedKey.Properties.Version, actualLatestKey.Value.Properties.Version);
    }

    [Fact]
    public async Task GetKeyByOlderVersionWorks()
    {
        string expectedName = Guid.NewGuid().ToString();
        KeyVaultKey expected = await CreateKeyAsync(expectedName);

        await CreateKeyAsync(expectedName);

        Response<KeyVaultKey> actualLatest = await client
            .GetKeyAsync(expectedName, expected.Properties.Version);
        Assert.Equal(expected.Id, actualLatest.Value.Id);
        Assert.Equal(expected.Properties.Version, actualLatest.Value.Properties.Version);
    }

    [Fact]
    public async Task GetKeyForInexistentKeyResultsIn404()
    {
        string name = Guid.NewGuid().ToString();

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAsync(name));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task GetKeyByVersionForInexistentVersionResultsIn404()
    {
        string name = Guid.NewGuid().ToString();
        string version = Guid.NewGuid().ToString();

        await CreateKeyAsync(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAsync(name, version));
        Assert.Equal(404, ex.Status);
    }
}

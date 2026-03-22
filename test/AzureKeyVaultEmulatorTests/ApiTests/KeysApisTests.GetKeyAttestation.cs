using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task GetKeyAttestationByNameOnlyGetsLatestVersion()
    {
        string expectedName = Guid.NewGuid().ToString();

        KeyVaultKey key1 = await CreateKeyAsync(expectedName);
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAttestationAsync(expectedName));
        Assert.Equal(400, ex.Status);

        KeyVaultKey key2 = await CreateKeyAsync(expectedName);
        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAttestationAsync(expectedName));
        Assert.Equal(400, ex.Status);
    }

    [Fact]
    public async Task GetKeyAttestationByVersionWorks()
    {
        string expectedName = Guid.NewGuid().ToString();
        KeyVaultKey expectedKey = await CreateKeyAsync(expectedName);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAttestationAsync(expectedName, expectedKey.Properties.Version));
        Assert.Equal(400, ex.Status);
    }

    [Fact]
    public async Task GetKeyAttestationByOlderVersionWorks()
    {
        string expectedName = Guid.NewGuid().ToString();
        KeyVaultKey expected = await CreateKeyAsync(expectedName);

        await CreateKeyAsync(expectedName);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAttestationAsync(expectedName, expected.Properties.Version));
        Assert.Equal(400, ex.Status);
    }

    [Fact]
    public async Task GetKeyAttestationForInexistentKeyResultsIn404()
    {
        string name = Guid.NewGuid().ToString();

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAttestationAsync(name));
        Assert.Equal(400, ex.Status);
    }

    [Fact]
    public async Task GetKeyAttestationByVersionForInexistentVersionResultsIn404()
    {
        string name = Guid.NewGuid().ToString();
        string version = Guid.NewGuid().ToString();

        await CreateKeyAsync(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAttestationAsync(name, version));
        Assert.Equal(400, ex.Status);
    }
}

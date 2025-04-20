using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests;

partial class KeysApisTests
{
    [Fact]
    public async Task GetKeyVersionsWorks()
    {
        string name = Guid.NewGuid().ToString();
        await CreateKeyAsync(name);
        await CreateKeyAsync(name);

        AsyncPageable<KeyProperties> versions = client.GetPropertiesOfKeyVersionsAsync(name);
        int count = 0;
        await foreach (KeyProperties version in versions)
        {
            Assert.Equal(name, version.Name);
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetKeyVersionsReturns404ForInexistentKey()
    {
        string name = Guid.NewGuid().ToString();

        AsyncPageable<KeyProperties> versions = client.GetPropertiesOfKeyVersionsAsync(name);
        IAsyncEnumerator<KeyProperties> enumerator = versions.GetAsyncEnumerator();
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => enumerator.MoveNextAsync().AsTask());
        Assert.Equal(404, ex.Status);
    }
}

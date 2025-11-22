using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.ApiTests;

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
    public async Task GetKeyVersionsReturnsEmptyPageForInexistentKey()
    {
        string name = Guid.NewGuid().ToString();

        AsyncPageable<KeyProperties> versions = client.GetPropertiesOfKeyVersionsAsync(name);
        IAsyncEnumerator<KeyProperties> enumerator = versions.GetAsyncEnumerator();
        Assert.False(await enumerator.MoveNextAsync());
        Assert.Empty(versions.ToBlockingEnumerable(default));
    }
}

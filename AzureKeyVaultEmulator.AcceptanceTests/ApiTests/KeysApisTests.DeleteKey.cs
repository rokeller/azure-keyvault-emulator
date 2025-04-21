using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task DeleteKeyWorks()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultKey v1 = await CreateKeyAsync(name);
        KeyVaultKey v2 = await CreateKeyAsync(name);

        await client.StartDeleteKeyAsync(name);

        RequestFailedException ex;

        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAsync(name, v1.Properties.Version));
        Assert.Equal(404, ex.Status);

        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyAsync(name, v2.Properties.Version));
        Assert.Equal(404, ex.Status);
    }
}


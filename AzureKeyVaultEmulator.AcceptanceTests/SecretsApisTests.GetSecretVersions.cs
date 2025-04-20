using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task GetSecretVersionsWorks()
    {
        string name = Guid.NewGuid().ToString();
        await CreateSecretAsync(name);
        await CreateSecretAsync(name);

        AsyncPageable<SecretProperties> versions = client.GetPropertiesOfSecretVersionsAsync(name);
        int count = 0;
        await foreach (SecretProperties version in versions)
        {
            Assert.Equal(name, version.Name);
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetSecretVersionsReturns404ForInexistentSecret()
    {
        string name = Guid.NewGuid().ToString();

        AsyncPageable<SecretProperties> versions = client.GetPropertiesOfSecretVersionsAsync(name);
        IAsyncEnumerator<SecretProperties> enumerator = versions.GetAsyncEnumerator();
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => enumerator.MoveNextAsync().AsTask());
        Assert.Equal(404, ex.Status);
    }
}

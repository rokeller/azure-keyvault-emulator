using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

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
    public async Task GetSecretVersionsReturnsEmptyPageForInexistentSecret()
    {
        string name = Guid.NewGuid().ToString();

        AsyncPageable<SecretProperties> versions = client.GetPropertiesOfSecretVersionsAsync(name);
        IAsyncEnumerator<SecretProperties> enumerator = versions.GetAsyncEnumerator();
        Assert.False(await enumerator.MoveNextAsync());
        Assert.Empty(versions.ToBlockingEnumerable(default));
    }
}

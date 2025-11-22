using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace AzureKeyVaultEmulator.ApiTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task DeleteSecretWorks()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultSecret v1 = await CreateSecretAsync(name);
        KeyVaultSecret v2 = await CreateSecretAsync(name);

        await client.StartDeleteSecretAsync(name);

        RequestFailedException ex;

        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecretAsync(name, v1.Properties.Version));
        Assert.Equal(404, ex.Status);

        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecretAsync(name, v2.Properties.Version));
        Assert.Equal(404, ex.Status);
    }
}

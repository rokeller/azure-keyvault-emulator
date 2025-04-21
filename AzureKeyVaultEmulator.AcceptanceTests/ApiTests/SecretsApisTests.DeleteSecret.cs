using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task DeleteSecretWorks()
    {
        string name = Guid.NewGuid().ToString();
        Response<KeyVaultSecret> v1 = await CreateSecretAsync(name);
        Response<KeyVaultSecret> v2 = await CreateSecretAsync(name);

        await client.StartDeleteSecretAsync(name);

        RequestFailedException ex;

        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecretAsync(name, v1.Value.Properties.Version));
        Assert.Equal(404, ex.Status);

        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecretAsync(name, v2.Value.Properties.Version));
        Assert.Equal(404, ex.Status);
    }
}

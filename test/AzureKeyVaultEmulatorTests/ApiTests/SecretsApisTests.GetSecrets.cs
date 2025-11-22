using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace AzureKeyVaultEmulator.ApiTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task GetSecretsWorks()
    {
        string name = Guid.NewGuid().ToString();
        await CreateSecretAsync(name);

        bool found = false;
        AsyncPageable<SecretProperties> secrets = client.GetPropertiesOfSecretsAsync();
        await foreach (SecretProperties secret in secrets)
        {
            if (secret.Name == name)
            {
                found = true;
                break;
            }
        }

        Assert.True(found);
    }
}

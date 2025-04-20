using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests;

partial class KeysApisTests
{
    [Fact]
    public async Task GetKeysWorks()
    {
        string name = Guid.NewGuid().ToString();
        await CreateKeyAsync(name);

        bool found = false;
        AsyncPageable<KeyProperties> keys = client.GetPropertiesOfKeysAsync();
        await foreach (KeyProperties key in keys)
        {
            if (key.Name == name)
            {
                found = true;
                break;
            }
        }

        Assert.True(found);
    }
}

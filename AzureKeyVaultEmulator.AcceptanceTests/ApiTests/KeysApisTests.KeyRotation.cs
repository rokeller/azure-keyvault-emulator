using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task RotateKeyFails_Unsupported()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultKey key = await CreateKeyAsync(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RotateKeyAsync(name));
        Assert.Equal(500, ex.Status);
    }

    [Fact]
    public async Task GetKeyRotationPolicyFails_Unsupported()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultKey key = await CreateKeyAsync(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyRotationPolicyAsync(name));
        Assert.Equal(500, ex.Status);
    }
    [Fact]
    public async Task UpdateKeyRotationPolicyFails_Unsupported()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultKey key = await CreateKeyAsync(name);
        KeyRotationPolicy policy = new();

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.UpdateKeyRotationPolicyAsync(name, policy));
        Assert.Equal(500, ex.Status);
    }
}


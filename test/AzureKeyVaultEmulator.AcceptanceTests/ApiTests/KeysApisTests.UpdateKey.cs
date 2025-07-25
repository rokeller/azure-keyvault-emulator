using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task UpdateKeyWorks()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultKey initial = await CreateKeyAsync(name); ;

        Response<KeyVaultKey> update1 = await client
            .UpdateKeyPropertiesAsync(new(initial.Id));
        Assert.Equal(initial.Properties.CreatedOn, update1.Value.Properties.CreatedOn);
        Utils.AssertInLastTwoSeconds(update1.Value.Properties.UpdatedOn);
        Assert.True(update1.Value.Properties.Enabled);
        Assert.Null(update1.Value.Properties.NotBefore);
        Assert.Null(update1.Value.Properties.ExpiresOn);
        Assert.Empty(update1.Value.KeyOperations);
        Assert.Empty(update1.Value.Properties.Tags);

        Response<KeyVaultKey> update2 = await client
            .UpdateKeyPropertiesAsync(
                new(initial.Id), [KeyOperation.Encrypt, KeyOperation.Decrypt]);
        Assert.Equal(initial.Properties.CreatedOn, update2.Value.Properties.CreatedOn);
        Utils.AssertInLastTwoSeconds(update2.Value.Properties.UpdatedOn);
        Assert.True(update2.Value.Properties.Enabled);
        Assert.Null(update2.Value.Properties.NotBefore);
        Assert.Null(update2.Value.Properties.ExpiresOn);
        Assert.Empty(update1.Value.KeyOperations);
        Assert.Empty(update2.Value.Properties.Tags);

        DateTimeOffset nbf = DateTimeOffset.Now;
        Response<KeyVaultKey> update3 = await client
            .UpdateKeyPropertiesAsync(new(initial.Id)
            {
                NotBefore = nbf,
            });
        Assert.Equal(initial.Properties.CreatedOn, update3.Value.Properties.CreatedOn);
        Utils.AssertInLastTwoSeconds(update3.Value.Properties.UpdatedOn);
        Assert.Null(update3.Value.Properties.Enabled);
        Utils.AssertInLastTwoSeconds(update3.Value.Properties.NotBefore);
        Assert.Null(update3.Value.Properties.ExpiresOn);
        Assert.Empty(update3.Value.Properties.Tags);

        Response<KeyVaultKey> update4 = await client
            .UpdateKeyPropertiesAsync(new KeyProperties(initial.Id)
                .WithTag("environment", "test"));
        Assert.Equal(initial.Properties.CreatedOn, update4.Value.Properties.CreatedOn);
        Utils.AssertInLastTwoSeconds(update4.Value.Properties.UpdatedOn);
        Assert.Null(update4.Value.Properties.Enabled);
        Utils.AssertInLastTwoSeconds(update4.Value.Properties.NotBefore);
        Assert.Null(update4.Value.Properties.ExpiresOn);
        var tag = Assert.Single(update4.Value.Properties.Tags);
        Assert.Equal("environment", tag.Key);
        Assert.Equal("test", tag.Value);
    }

    [Fact]
    public async Task UpdateKeyResultsIn404WhenKeyNotFound()
    {
        string name = Guid.NewGuid().ToString();
        string version = Guid.NewGuid().ToString("N");

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
                    () => client.UpdateKeyPropertiesAsync(new(
                        new Uri($"https://localhost.vault.azure.net/keys/{name}/{version}"))));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task UpdateOfLatestVersionAffectsGetWithoutVersionToo()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultKey initial = await CreateKeyAsync(name);
        Assert.True(initial.Properties.Enabled);
        string version = initial.Properties.Version;

        await client.UpdateKeyPropertiesAsync(new(initial.Id)
        {
            Enabled = false,
        });

        KeyVaultKey updated = await client.GetKeyAsync(name);
        Assert.False(updated.Properties.Enabled);
        Assert.Equal(version, updated.Properties.Version);
    }
}

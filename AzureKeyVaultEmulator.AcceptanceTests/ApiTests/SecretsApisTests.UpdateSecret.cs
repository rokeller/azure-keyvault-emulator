using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task UpdateSecretWorks()
    {
        string name = Guid.NewGuid().ToString();
        string value = Guid.NewGuid().ToString();
        Response<KeyVaultSecret> initial = await client.SetSecretAsync(name, value);

        Response<SecretProperties> update1 = await client
            .UpdateSecretPropertiesAsync(new SecretProperties(initial.Value.Id));
        Assert.Equal(initial.Value.Properties.CreatedOn, update1.Value.CreatedOn);
        Utils.AssertInLastTwoSeconds(update1.Value.UpdatedOn);
        Assert.Null(update1.Value.ContentType);
        Assert.Null(update1.Value.Enabled);
        Assert.Null(update1.Value.NotBefore);
        Assert.Null(update1.Value.ExpiresOn);
        Assert.Empty(update1.Value.Tags);

        Response<SecretProperties> update2 = await client
            .UpdateSecretPropertiesAsync(new SecretProperties(initial.Value.Id)
            {
                ContentType = "text/plain",
            });
        Assert.Equal(initial.Value.Properties.CreatedOn, update2.Value.CreatedOn);
        Utils.AssertInLastTwoSeconds(update2.Value.UpdatedOn);
        Assert.Equal("text/plain", update2.Value.ContentType);
        Assert.Null(update2.Value.Enabled);
        Assert.Null(update2.Value.NotBefore);
        Assert.Null(update2.Value.ExpiresOn);
        Assert.Empty(update2.Value.Tags);

        DateTimeOffset nbf = DateTimeOffset.Now;
        Response<SecretProperties> update3 = await client
            .UpdateSecretPropertiesAsync(new SecretProperties(initial.Value.Id)
            {
                NotBefore = nbf,
            });
        Assert.Equal(initial.Value.Properties.CreatedOn, update3.Value.CreatedOn);
        Utils.AssertInLastTwoSeconds(update3.Value.UpdatedOn);
        Assert.Equal("text/plain", update3.Value.ContentType);
        Assert.Null(update3.Value.Enabled);
        Utils.AssertInLastTwoSeconds(update3.Value.NotBefore);
        Assert.Null(update3.Value.ExpiresOn);
        Assert.Empty(update3.Value.Tags);

        Response<SecretProperties> update4 = await client
            .UpdateSecretPropertiesAsync(new SecretProperties(initial.Value.Id)
                .WithTag("environment", "test"));
        Assert.Equal(initial.Value.Properties.CreatedOn, update4.Value.CreatedOn);
        Utils.AssertInLastTwoSeconds(update4.Value.UpdatedOn);
        Assert.Equal("text/plain", update4.Value.ContentType);
        Assert.Null(update4.Value.Enabled);
        Utils.AssertInLastTwoSeconds(update4.Value.NotBefore);
        Assert.Null(update4.Value.ExpiresOn);
        var tag = Assert.Single(update4.Value.Tags);
        Assert.Equal("environment", tag.Key);
        Assert.Equal("test", tag.Value);

        Response<KeyVaultSecret> final = await client
            .GetSecretAsync(name, initial.Value.Properties.Version);
        Assert.Equal(value, final.Value.Value);
    }

    [Fact]
    public async Task UpdateSecretResultsIn404WhenSecretNotFound()
    {
        string name = Guid.NewGuid().ToString();
        string version = Guid.NewGuid().ToString("N");

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
                    () => client.UpdateSecretPropertiesAsync(new SecretProperties(
                        new Uri($"https://localhost.vault.azure.net/secrets/{name}/{version}"))));
        Assert.Equal(404, ex.Status);
    }
}

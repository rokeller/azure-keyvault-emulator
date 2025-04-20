using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task SetSecretCreatesNewSecret()
    {
        KeyVaultSecret secret = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
        {
            Properties =
            {
                ContentType = "text/plain",
                Enabled = true,
                NotBefore = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
                Tags =
                {
                    { "environment", "local" },
                    { "testing", "true" },
                },
            },
        };

        Response<KeyVaultSecret> result = await client.SetSecretAsync(secret);
        Assert.NotNull(result);

        KeyVaultSecret createdSecret = result.Value;
        Assert.NotNull(createdSecret);

        Assert.NotNull(createdSecret.Id);
        Assert.Equal(secret.Value, createdSecret.Value);
        Assert.Equal(secret.Properties.ContentType, createdSecret.Properties.ContentType);
        Assert.Equal(secret.Properties.Enabled, createdSecret.Properties.Enabled);
        Assert.Equal(secret.Properties.Managed, createdSecret.Properties.Managed);
        Assert.Equal(secret.Properties.NotBefore.Value.ToUnixTimeSeconds(),
            createdSecret.Properties.NotBefore.GetValueOrDefault().ToUnixTimeSeconds());
        Assert.Equal(secret.Properties.ExpiresOn.Value.ToUnixTimeSeconds(),
            createdSecret.Properties.ExpiresOn.GetValueOrDefault().ToUnixTimeSeconds());
        Assert.NotNull(createdSecret.Properties.Version);
        Assert.Equal("local", createdSecret.Properties.Tags["environment"]);
        Assert.Equal("true", createdSecret.Properties.Tags["testing"]);

        Utils.AssertInLastTwoSeconds(createdSecret.Properties.CreatedOn);
        Utils.AssertInLastTwoSeconds(createdSecret.Properties.UpdatedOn);
    }

    [Fact]
    public async Task SetSecretWithNameAndValueOnlyCreatesNewSecret()
    {
        string name = Guid.NewGuid().ToString();
        string value = Guid.NewGuid().ToString();

        Response<KeyVaultSecret> result = await client.SetSecretAsync(name, value);
        Assert.NotNull(result);

        KeyVaultSecret createdSecret = result.Value;
        Assert.NotNull(createdSecret);

        Assert.NotNull(createdSecret.Id);
        Assert.Equal(value, createdSecret.Value);
        Assert.Null(createdSecret.Properties.ContentType);
        Assert.Null(createdSecret.Properties.Enabled);
        Assert.False(createdSecret.Properties.Managed);
        Assert.Null(createdSecret.Properties.NotBefore);
        Assert.Null(createdSecret.Properties.ExpiresOn);
        Assert.NotNull(createdSecret.Properties.Version);
        Assert.Empty(createdSecret.Properties.Tags);

        Utils.AssertInLastTwoSeconds(createdSecret.Properties.CreatedOn);
        Utils.AssertInLastTwoSeconds(createdSecret.Properties.UpdatedOn);
    }
}

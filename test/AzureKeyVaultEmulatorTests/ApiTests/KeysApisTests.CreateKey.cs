using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.ApiTests;

partial class KeysApisTests
{
    [Theory]
    [InlineData("EC")]
    [InlineData("RSA")]
    [InlineData("oct")]
    public async Task CreateKeyWorksWithDefaults(string keyType)
    {
        KeyType type = new(keyType);
        CreateKeyOptions options = new CreateKeyOptions()
        {
            Enabled = true,
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
            NotBefore = DateTimeOffset.UtcNow,
            Tags =
            {
                { "environment", "test" },
            },
        }
            .WithKeyOperation(KeyOperation.WrapKey)
            .WithKeyOperation(KeyOperation.UnwrapKey);
        string keyName = $"{keyType}-{Guid.NewGuid():N}";

        Response<KeyVaultKey> result = await client.CreateKeyAsync(keyName, type, options);
        KeyVaultKey key = result.Value;

        Assert.NotNull(key.Id);
        Assert.Matches($"^https://localhost.vault.azure.net/keys/{keyName}/[0-9a-f]{{32}}$", key.Id.ToString());
        Assert.Equal(keyName, key.Name);
        Assert.Equal(keyType, key.KeyType.ToString(), ignoreCase: true);
        Assert.True(key.Properties.Enabled);
        Assert.Matches("^[0-9a-f]{32}$", key.Properties.Version);
        Assert.False(key.Properties.Managed);
        Assert.Equal("test", key.Properties.Tags["environment"]);
        Assert.Equal(options.ExpiresOn!.Value.ToUnixTimeSeconds(),
            key.Properties.ExpiresOn!.Value.ToUnixTimeSeconds());
        Assert.Equal(options.NotBefore!.Value.ToUnixTimeSeconds(),
            key.Properties.NotBefore!.Value.ToUnixTimeSeconds());

        Assert.Collection(key.KeyOperations,
            (op) => Assert.Equal(KeyOperation.WrapKey, op),
            (op) => Assert.Equal(KeyOperation.UnwrapKey, op)
        );
    }

    [Theory]
    [InlineData("EC-HSM")]
    [InlineData("RSA-HSM")]
    [InlineData("oct-HSM")]
    public async Task CreateKeyFailsForHsmKeys(string keyType)
    {
        KeyType type = new(keyType);
        CreateKeyOptions options = new()
        {
            Enabled = true,
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(1),
            NotBefore = DateTimeOffset.UtcNow,
            Tags =
            {
                { "environment", "test" },
            },
        };
        string keyName = $"{keyType}-{Guid.NewGuid():N}";

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.CreateKeyAsync(keyName, type, options));
        Assert.Equal(500, ex.Status);
    }

    [Theory]
    [InlineData("P-256")]
    [InlineData("P-384")]
    [InlineData("P-521")]
    public async Task CreateEcKeyWorksForCurve(string curveName)
    {
        string keyName = Guid.NewGuid().ToString("N");
        CreateEcKeyOptions options = new(keyName)
        {
            CurveName = new KeyCurveName(curveName),
        };
        Response<KeyVaultKey> result = await client.CreateEcKeyAsync(options);
        KeyVaultKey key = result.Value;
        Assert.Equal(KeyType.Ec, key.KeyType);
        Assert.Equal(new KeyCurveName(curveName), key.Key.CurveName);
    }

    [Theory]
    [InlineData("P-256K")]
    public async Task CreateEcKeyFailsForUnsupportedCurve(string curveName)
    {
        string keyName = Guid.NewGuid().ToString("N");
        CreateEcKeyOptions options = new(keyName)
        {
            CurveName = new KeyCurveName(curveName),
        };

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.CreateEcKeyAsync(options));
        Assert.Equal(500, ex.Status);
    }

    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    [InlineData(4096)]
    public async Task CreateRsaKeyWorksForKeySize(int keySize)
    {
        string keyName = Guid.NewGuid().ToString("N");
        CreateRsaKeyOptions options = new(keyName)
        {
            KeySize = keySize,
        };
        Response<KeyVaultKey> result = await client.CreateRsaKeyAsync(options);
        KeyVaultKey key = result.Value;
        Assert.Equal(KeyType.Rsa, key.KeyType);
        Assert.NotNull(key.Key.N);
        Assert.Equal(keySize, key.Key.N.Length * 8);
    }
}


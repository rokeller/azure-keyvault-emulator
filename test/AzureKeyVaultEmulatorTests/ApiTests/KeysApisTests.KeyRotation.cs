using System;
using System.Threading.Tasks;
using System.Xml;
using AutoFixture.Xunit2;
using Azure;
using Azure.Security.KeyVault.Keys;
using Xunit;

namespace AzureKeyVaultEmulator.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task RotateKeyResultsIn404ForInexistentKey()
    {
        string name = Guid.NewGuid().ToString();
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RotateKeyAsync(name));
        Assert.Equal(404, ex.Status);
    }

    [Theory]
    [InlineData("EC")]
    [InlineData("RSA")]
    [InlineData("oct")]
    public async Task RotateKeyCreatesNewVersionOfKeyWithoutExpirationWhenRotationPolicyIsMissing(string kty)
    {
        string name = Guid.NewGuid().ToString();
        CreateKeyOptions opts = new()
        {
            Tags = { { "test", "yes" } },
            Enabled = true,
        };
        opts.WithKeyOperation(KeyOperation.Sign).WithKeyOperation(KeyOperation.Verify);
        KeyVaultKey orig = await client.CreateKeyAsync(name, new KeyType(kty), opts);

        // Rotate the key and verify some key properties.
        KeyVaultKey rotated = await client.RotateKeyAsync(name);
        Assert.True(rotated.Properties.UpdatedOn >= orig.Properties.UpdatedOn);
        Assert.Equal(orig.Properties.Tags, rotated.Properties.Tags);
        Assert.Equal(orig.KeyOperations, rotated.KeyOperations);
        Assert.Equal(orig.Properties.Enabled, rotated.Properties.Enabled);
    }

    [Theory]
    [InlineData("EC")]
    [InlineData("RSA")]
    [InlineData("oct")]
    public async Task RotateKeyCreatesNewVersionOfKeyWithoutExpirationWhenRotationPolicyIsEmpty(string kty)
    {
        string name = Guid.NewGuid().ToString();
        CreateKeyOptions opts = new()
        {
            Tags = { { "test", "yes" } },
            Enabled = true,
        };
        opts.WithKeyOperation(KeyOperation.Sign).WithKeyOperation(KeyOperation.Verify);
        KeyVaultKey orig = await client.CreateKeyAsync(name, new KeyType(kty), opts);
        await client.UpdateKeyRotationPolicyAsync(name, new());

        // Rotate the key and verify some key properties.
        KeyVaultKey rotated = await client.RotateKeyAsync(name);
        Assert.True(rotated.Properties.UpdatedOn >= orig.Properties.UpdatedOn);
        Assert.Equal(orig.Properties.Tags, rotated.Properties.Tags);
        Assert.Equal(orig.KeyOperations, rotated.KeyOperations);
        Assert.Equal(orig.Properties.Enabled, rotated.Properties.Enabled);
        Assert.False(rotated.Properties.ExpiresOn.HasValue);
    }

    [Theory]
    [InlineAutoData("EC")]
    [InlineAutoData("RSA")]
    [InlineAutoData("oct")]
    public async Task RotateKeyCreatesNewVersionOfKeyWithExpirationFromRotationPolicy(
        string kty,
        byte numDays)
    {
        string name = Guid.NewGuid().ToString();
        CreateKeyOptions opts = new()
        {
            Tags = { { "test", "yes" } },
            Enabled = true,
        };
        opts.WithKeyOperation(KeyOperation.Sign).WithKeyOperation(KeyOperation.Verify);
        KeyVaultKey orig = await client.CreateKeyAsync(name, new KeyType(kty), opts);

        DateTimeOffset min = DateTimeOffset.UtcNow.AddDays(numDays).AddSeconds(-1);
        KeyRotationPolicy policy = new()
        {
            ExpiresIn = XmlConvert.ToString(TimeSpan.FromDays(numDays)),
        };
        await client.UpdateKeyRotationPolicyAsync(name, policy);
        DateTimeOffset max = DateTimeOffset.UtcNow.AddDays(numDays).AddSeconds(1);

        // Rotate the key and verify some key properties.
        KeyVaultKey rotated = await client.RotateKeyAsync(name);
        Assert.True(rotated.Properties.UpdatedOn >= orig.Properties.UpdatedOn);
        Assert.Equal(orig.Properties.Tags, rotated.Properties.Tags);
        Assert.Equal(orig.KeyOperations, rotated.KeyOperations);
        Assert.Equal(orig.Properties.Enabled, rotated.Properties.Enabled);

        Assert.True(rotated.Properties.ExpiresOn.HasValue);
        Assert.InRange(rotated.Properties.ExpiresOn.Value, min, max);
    }

    [Fact]
    public async Task GetKeyRotationPolicyResultsIn404ForInexistentKey()
    {
        string name = Guid.NewGuid().ToString();
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyRotationPolicyAsync(name));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task GetKeyRotationPolicyResultsIn404WithoutPolicyPresent()
    {
        string name = Guid.NewGuid().ToString();
        await CreateKeyAsync(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetKeyRotationPolicyAsync(name));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task GetKeyRotationPolicyReturnsPolicyWhenPresent()
    {
        string name = Guid.NewGuid().ToString();
        await CreateKeyAsync(name);
        KeyRotationPolicy policy = new()
        {
            ExpiresIn = "P34D",
        };
        policy.LifetimeActions.Add(
            new(KeyRotationPolicyAction.Rotate)
            {
                TimeBeforeExpiry = "P7D",
            });

        await client.UpdateKeyRotationPolicyAsync(name, policy);
        KeyRotationPolicy result = await client.GetKeyRotationPolicyAsync(name);
        Assert.Equal("P34D", result.ExpiresIn);
        Assert.Single(result.LifetimeActions,
            p => p.Action == KeyRotationPolicyAction.Rotate
                && p.TimeBeforeExpiry == "P7D");
    }

    [Fact]
    public async Task UpdateKeyRotationPolicyFailsResultsIn404ForInexistentKey()
    {
        string name = Guid.NewGuid().ToString();
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.UpdateKeyRotationPolicyAsync(name, new()));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task UpdateKeyRotationPolicyKeepsCreatedDate()
    {
        string name = Guid.NewGuid().ToString();
        await CreateKeyAsync(name);
        KeyRotationPolicy policy = new()
        {
            ExpiresIn = "P34D",
        };
        policy.LifetimeActions.Add(
            new(KeyRotationPolicyAction.Rotate)
            {
                TimeBeforeExpiry = "P7D",
            });

        KeyRotationPolicy p1 = await client.UpdateKeyRotationPolicyAsync(name, policy);
        Assert.Equal("P34D", p1.ExpiresIn);

        await Task.Delay(TimeSpan.FromSeconds(1));

        policy.ExpiresIn = "P45D";
        KeyRotationPolicy p2 = await client.UpdateKeyRotationPolicyAsync(name, policy);
        Assert.Equal("P45D", p2.ExpiresIn);
        Assert.Equal(p1.CreatedOn, p2.CreatedOn);
        Assert.True(p1.UpdatedOn < p2.UpdatedOn,
            $"p1 ({p1.UpdatedOn}) must have been last updated before p2 ({p2.UpdatedOn})");
    }
}


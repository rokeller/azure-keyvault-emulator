using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Keys;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

public sealed class RngApisTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new();

    private readonly KeyClient client;

    public RngApisTests()
    {
        factory.ClientOptions.BaseAddress = new("https://localhost.vault.azure.net/");
        KeyClientOptions options = new()
        {
            Transport = new HttpClientTransport(factory.CreateClient()),
            RetryPolicy = new RetryPolicy(maxRetries: 0),
        };

        client = new(factory.ClientOptions.BaseAddress, new LocalTokenCredential(), options);
    }

    public void Dispose()
    {
        ((IDisposable)factory).Dispose();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(128)]
    public async Task GetRandomBytesWorksWhenCountInRange(int count)
    {
        Response<byte[]> res = await client.GetRandomBytesAsync(count);
        Assert.Equal(count, res.Value.Length);
    }

    [Theory]
    [InlineData(129)]
    public async Task GetRandomBytesFailsWhenCountOutOfRange(int count)
    {
        await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetRandomBytesAsync(count));
    }
}


using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task BackupKeyNotSupported()
    {
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.BackupKeyAsync(Guid.NewGuid().ToString()));
        Assert.Equal(500, ex.Status);
    }

    [Fact]
    public async Task RestoreKeyNotSupported()
    {
        byte[] backup = new byte[128];
        rng.GetBytes(backup);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreKeyBackupAsync(backup));
        Assert.Equal(500, ex.Status);
    }

    [Fact()]

    public async Task RestoreKeyNotSupported_HttpDirectly()
    {
        byte[] backup = new byte[128];
        rng.GetBytes(backup);

        HttpRequestMessage req = new(HttpMethod.Post, "/keys/restore?api-version=7.4")
        {
            Content = JsonContent.Create(new { value = WebEncoders.Base64UrlEncode(backup) }),
        };
        req.Headers.Authorization = new("Bearer", LocalTokenCredential.Token);
        HttpResponseMessage resp = await httpClient.SendAsync(req);
        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }
}

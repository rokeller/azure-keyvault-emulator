using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace AzureKeyVaultEmulator.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task BackupKeyCauses404ForInexistentSecrets()
    {
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.BackupKeyAsync(Guid.NewGuid().ToString()));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task BackupKeyWorks()
    {
        string name = Guid.NewGuid().ToString();
        await CreateKeyAsync(name);

        byte[] backup = await client.BackupKeyAsync(name);
        Assert.NotNull(backup);
    }

    [Fact]
    public async Task RestoreKeyRandomValueCausesServerError()
    {
        byte[] backup = new byte[128];
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreKeyBackupAsync(backup));
        Assert.Equal(500, ex.Status);
    }

    [Fact]
    public async Task RestoreKeyForExistingSecretCauses409()
    {
        string name = Guid.NewGuid().ToString();
        await CreateEcKeyAsync(name);
        byte[] backup = await client.BackupKeyAsync(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreKeyBackupAsync(backup));
        Assert.Equal(409, ex.Status);
    }

    [Theory]
    // Empty versions lists
    [InlineData("H4sIAD-lCGgA_y3JLQ6AQAwG0bt8GoOt5ADIGoJoSAXZ7E-2RRHu3grcm8yLTa7yDNZpd28g8IoFu1RNu5pn_dNAx_kFUYrxpDIAAAA")]
    // Null versions lists
    [InlineData("H4sIAGWlCGgA_w3Gqw0AIQwG4F1-fQaLZABkfUMqCOER2lOE3Yv7DhKX9i-SrXUORFDAh8xdnk3UcB2T-d02JAAAAA")]
    // Space-only secret name
    [InlineData("H4sIAIClCGgA_w3GKwoAIAwG4KuMP1usRg9gXB-yIOIDxSTefbbvIkquZ7KuXUZHAHs4JGn6TYRnz3NahSIAAAA")]
    // Null secret name
    [InlineData("H4sIAJalCGgA_wWAMQoAEABF7_JuYDU6xN8lgxQik9z990i59LtU92lzEFHgG-A-x5cWAAAA")]
    // Unsupported backup version
    [InlineData("H4sIAMSlCGgA_wWAMQoAEABF7_JOYDY6xN8lgxQik9z990i59LtU92lzEFHgG9dUBZYWAAAA")]
    // Null backup version
    [InlineData("H4sIAPqlCGgA_wWAQQkAAAgDu1wM2-jD78DvsPswK1FMHx_v9Sv-DQAAAA")]
    public async Task RestoreKeyWithCorruptedBackupCauses400(string backupBase64)
    {
        byte[] backup = WebEncoders.Base64UrlDecode(backupBase64);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreKeyBackupAsync(backup));
        Assert.Equal(400, ex.Status);
    }

    [Fact]
    public async Task RestoreKeyWorks()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultKey initial = await CreateRsaKeyAsync(name);

        byte[] backup = await client.BackupKeyAsync(name);
        Assert.NotNull(backup);

        await client.StartDeleteKeyAsync(name);

        // Now restore the backup.
        KeyVaultKey restored = await client.RestoreKeyBackupAsync(backup);
        Assert.Equivalent(initial, restored);
    }

    [Fact]
    // The .Net SDK adds double slashes to the URL which isn't technically correct
    // but anyway supported by Key Vault. Let's test that it also works with the
    // correct URL.
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

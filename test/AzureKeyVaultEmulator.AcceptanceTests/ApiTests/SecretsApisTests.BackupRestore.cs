using System;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task BackupSecretCauses404ForInexistentSecrets()
    {
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.BackupSecretAsync(Guid.NewGuid().ToString()));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task BackupSecretWorks()
    {
        string name = Guid.NewGuid().ToString();
        await CreateSecretAsync(name);

        byte[] backup = await client.BackupSecretAsync(name);
        Assert.NotNull(backup);
    }

    [Fact]
    public async Task RestoreSecretRandomValueCausesServerError()
    {
        byte[] backup = new byte[128];
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreSecretBackupAsync(backup));
        Assert.Equal(500, ex.Status);
    }

    [Fact]
    public async Task RestoreSecretForExistingSecretCauses409()
    {
        string name = Guid.NewGuid().ToString();
        await CreateSecretAsync(name);
        byte[] backup = await client.BackupSecretAsync(name);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreSecretBackupAsync(backup));
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
    public async Task RestoreSecretWithCorruptedBackupCauses400(string backupBase64)
    {
        byte[] backup = WebEncoders.Base64UrlDecode(backupBase64);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreSecretBackupAsync(backup));
        Assert.Equal(400, ex.Status);
    }

    [Fact]
    public async Task RestoreSecretWorks()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultSecret initial = await CreateSecretAsync(name);

        byte[] backup = await client.BackupSecretAsync(name);
        Assert.NotNull(backup);

        await client.StartDeleteSecretAsync(name);

        // Now restore the backup.
        SecretProperties restored = await client.RestoreSecretBackupAsync(backup);
        Assert.Equivalent(initial.Properties, restored);
    }
}

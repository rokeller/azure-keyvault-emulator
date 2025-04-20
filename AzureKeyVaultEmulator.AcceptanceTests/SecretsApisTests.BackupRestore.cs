using System;
using System.Threading.Tasks;
using Azure;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests;

partial class SecretsApisTests
{
    [Fact]
    public async Task BackupSecretNotSupported()
    {
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.BackupSecretAsync(Guid.NewGuid().ToString()));
        Assert.Equal(500, ex.Status);
    }
    [Fact]

    public async Task RestoreSecretNotSupported()
    {
        byte[] backup = new byte[128];
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreSecretBackupAsync(backup));
        Assert.Equal(500, ex.Status);
    }
}

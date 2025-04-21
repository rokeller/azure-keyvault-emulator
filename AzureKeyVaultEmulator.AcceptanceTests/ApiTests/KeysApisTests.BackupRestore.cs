using System;
using System.Threading.Tasks;
using Azure;
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

    [Fact(Skip = "Misconfigured client uses 'keys//restore'")]

    public async Task RestoreKeyNotSupported()
    {
        byte[] backup = new byte[128];
        rng.GetBytes(backup);

        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.RestoreKeyBackupAsync(backup));
        Assert.Equal(500, ex.Status);
    }
}

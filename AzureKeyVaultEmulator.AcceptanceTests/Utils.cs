using System;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests;

internal static class Utils
{
    public static void AssertInLastTwoSeconds(DateTimeOffset? timestamp)
    {
        Assert.True(timestamp.HasValue);

        DateTimeOffset low = DateTimeOffset.Now.AddSeconds(-2);
        DateTimeOffset high = DateTimeOffset.Now;
        Assert.InRange(timestamp.Value, low, high);
    }
}


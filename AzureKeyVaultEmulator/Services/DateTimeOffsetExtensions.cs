using System;

namespace AzureKeyVaultEmulator.Services;

internal static class DateTimeOffsetExtensions
{
    public static int ToUnixSeconds(this DateTimeOffset timestamp) =>
        Convert.ToInt32(timestamp.ToUnixTimeSeconds());
}


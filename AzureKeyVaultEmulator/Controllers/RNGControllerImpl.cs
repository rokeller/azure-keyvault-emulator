using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AzureKeyVaultEmulator.Controllers;

internal sealed class RNGControllerImpl : IRNGController
{
    private readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

    public Task<ActionResult<RandomBytes>> GetRandomBytesAsync(
        string api_version,
        GetRandomBytesRequest body,
        CancellationToken cancellationToken = default)
    {
        if (body.Count > 128)
        {
            throw new NotSupportedException();
        }

        Span<byte> bytes = stackalloc byte[body.Count];
        rng.GetBytes(bytes);

        RandomBytes result = new()
        {
            Value = WebEncoders.Base64UrlEncode(bytes),
        };

        return Task.FromResult<ActionResult<RandomBytes>>(result);
    }
}

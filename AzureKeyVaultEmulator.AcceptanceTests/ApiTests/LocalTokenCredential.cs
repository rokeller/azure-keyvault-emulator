using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

internal sealed class LocalTokenCredential : TokenCredential
{
    private const string Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE4OTAyMzkwMjIsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjUwMDEvIn0.bHLeGTRqjJrmIJbErE-1Azs724E5ibzvrIc-UQL6pws";

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new(new AccessToken(Token, DateTimeOffset.MaxValue));
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new(Token, DateTimeOffset.MaxValue);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace AzureKeyVaultEmulator.ApiTests;

internal sealed class LocalTokenCredential : TokenCredential
{
    internal const string Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNzM1Njg5NjAwLCJleHAiOjQxMDI0NDQ4MDAsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0LyJ9.42D_zJ3qM02NM_ExWU9S9jvNGMfpop3YuWT9lFqJ5yU";

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new(new AccessToken(Token, DateTimeOffset.MaxValue));
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new(Token, DateTimeOffset.MaxValue);
    }
}

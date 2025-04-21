using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public async Task ImportKeyCreatesNewKey()
    {
        string name = Guid.NewGuid().ToString();
        JsonWebKey jwk = GetSampleJwk();
        ImportKeyOptions options = new(name, jwk);
        options.Properties.WithTag("source", "imported-from-test");

        KeyVaultKey key = await client.ImportKeyAsync(options);

        Assert.Equal(KeyType.Rsa, key.KeyType);
        Assert.Collection(key.KeyOperations,
            (op) => Assert.Equal(KeyOperation.Encrypt, op),
            (op) => Assert.Equal(KeyOperation.Decrypt, op)
            );
        (string tag, string value) = Assert.Single(key.Properties.Tags);
        Assert.Equal("source", tag);
        Assert.Equal("imported-from-test", value);
    }

    [Fact]
    public async Task ImportKeyUpdatesExistingKey()
    {
        string name = Guid.NewGuid().ToString();
        await CreateKeyAsync(name);

        JsonWebKey jwk = GetSampleJwk();
        ImportKeyOptions options = new(name, jwk);
        options.Properties.WithTag("source", "imported-from-test");

        KeyVaultKey imported = await client.ImportKeyAsync(options);

        Assert.Equal(KeyType.Rsa, imported.KeyType);
        Assert.Collection(imported.KeyOperations,
            (op) => Assert.Equal(KeyOperation.Encrypt, op),
            (op) => Assert.Equal(KeyOperation.Decrypt, op)
            );
        (string tag, string value) = Assert.Single(imported.Properties.Tags);
        Assert.Equal("source", tag);
        Assert.Equal("imported-from-test", value);

        AsyncPageable<KeyProperties> versions = client.GetPropertiesOfKeyVersionsAsync(name);
        int count = 0;
        bool foundImported = false;
        await foreach (KeyProperties version in versions)
        {
            if (version.Version == imported.Properties.Version)
            {
                foundImported = true;
            }
            Assert.Equal(name, version.Name);
            count++;
        }

        Assert.Equal(2, count);
        Assert.True(foundImported);
    }

    [Fact]
    public async Task ReleaseKeyFails_Unsupported()
    {
        string name = Guid.NewGuid().ToString();
        KeyVaultKey key = await CreateKeyAsync(name);

        ReleaseKeyOptions options = new(name, "test")
        {
            Version = key.Properties.Version,
        };
        RequestFailedException ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => client.ReleaseKeyAsync(options));
        Assert.Equal(500, ex.Status);
    }

    private static JsonWebKey GetSampleJwk()
    {
        // Sample from https://learn.microsoft.com/en-us/rest/api/keyvault/keys/import-key/import-key?view=rest-keyvault-keys-7.4&tabs=HTTP#import-key
        RSAParameters rsaParams = new()
        {
            Modulus = WebEncoders.Base64UrlDecode("nKAwarTrOpzd1hhH4cQNdVTgRF-b0ubPD8ZNVf0UXjb62QuAk3Dn68ESThcF7SoDYRx2QVcfoMC9WCcuQUQDieJF-lvJTSer1TwH72NBovwKlHvrXqEI0a6_uVYY5n-soGt7qFZNbwQLdWWA6PrbqTLIkv6r01dcuhTiQQAn6OWEa0JbFvWfF1kILQIaSBBBaaQ4R7hZs7-VQTHGD7J1xGteof4gw2VTiwNdcE8p5UG5b6S9KQwAeET4yB4KFPwQ3TDdzxJQ89mwYVi_sgAIggN54hTq4oEKYJHBOMtFGIN0_HQ60ZSUnpOi87xNC-8VFqnv4rfTQ7nkK6XMvjMVfw"),
            Exponent = WebEncoders.Base64UrlDecode("AQAB"),
            D = WebEncoders.Base64UrlDecode("GeT1_D5LAZa7qlC7WZ0DKJnOth8kcPrN0urTEFtWCbmHQWkAad_px_VUpGp0BWDDzENbXbQcu4QCCdf4crve5eXt8dVI86OSah-RpEdBq8OFsETIhg2Tmq8MbYTJexoynRcIC62xAaCmkFMmu931gQSvWnYWTEuOPgmD2oE_F-bP9TFlGRc69a6MSbtcSRyFTsd5KsUr40QS4zf2W4kZCOWejyLuxk88SXgUqcJx86Ulc1Ol1KkTBLadvReAZCyCMwKBlNRGw46BU_iK0vK7rTD9fmEd639Gjti6eLpnyQYpnVe8uGgwVU1fHBkAKyapWoEG6VMhMntcrvgukKLIsQ"),
            DP = WebEncoders.Base64UrlDecode("ZGnmWx-Nca71z9a9vvT4g02iv3S-3kSgmhl8JST09YQwK8tfiK7nXnNMtXJi2K4dLKKnLicGtCzB6W3mXdLcP2SUOWDOeStoBt8HEBT4MrI1psCKqnBum78WkHju90rBFj99amkP6UeQy5EASAzgmKQu2nUaUnRV0lYP8LHMCkE"),
            DQ = WebEncoders.Base64UrlDecode("dtpke0foFs04hPS6XYLA5lc7-1MAHfZKN4CkMAofwDqPmRQzCxpDJUk0gMWGJEdU_Lqfbg22Py44cci0dczH36NW3UU5BL86T2_SPPDOuyX7kDscrIJCdowxQCGJHGRBEozM_uTL46wu6UnUIv7m7cuGgodJyZBcdwpo6ziFink"),
            InverseQ = WebEncoders.Base64UrlDecode("Y9KD5GaHkAYmAqpOfAQUMr71QuAAaBb0APzMuUvoEYw39PD3_vJeh9HZ15QmJ8zCX10-nlzUB-bWwvK-rGcJXbK4pArilr5MiaYv7e8h5eW2zs2_itDJ6Oebi-wVbMhg7DvUTBbkCvPhhIedE4UlDQmMYP7RhzVVs7SfmkGs_DQ"),
            P = WebEncoders.Base64UrlDecode("v1jeCPnuJQM2PW2690Q9KJk0Ulok8VFGjkcHUHVi3orKdy7y_TCIWM6ZGvgFzI6abinzYbTEPKV4wFdMAwvOWmawXj5YrsoeB44_HXJ0ak_5_iP6XXR8MLGXbd0ZqsxvAZyzMj9vyle7EN2cBod6aenI2QZoRDucPvjPwZsZotk"),
            Q = WebEncoders.Base64UrlDecode("0Yv-Dj6qnvx_LL70lUnKA6MgHE_bUC4drl5ZNDDsUdUUYfxIK4G1rGU45kHGtp-Qg-Uyf9s52ywLylhcVE3jfbjOgEozlSwKyhqfXkLpMLWHqOKj9fcfYd4PWKPOgpzWsqjA6fJbBUMYo0CU2G9cWCtVodO7sBJVSIZunWrAlBc"),
        };
        RSA keyToImport = RSA.Create(rsaParams);
        JsonWebKey jwk = new(
            keyToImport,
            includePrivateParameters: true,
            [KeyOperation.Encrypt, KeyOperation.Decrypt]);

        return jwk;
    }
}


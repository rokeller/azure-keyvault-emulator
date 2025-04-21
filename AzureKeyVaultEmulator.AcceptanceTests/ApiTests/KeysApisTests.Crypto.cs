using System;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Xunit;

namespace AzureKeyVaultEmulator.AcceptanceTests.ApiTests;

partial class KeysApisTests
{
    [Fact]
    public Task EncryptDecryptWorks_Rsa15()
    {
        return EncryptDecryptWorks("rsa15",
                                   EncryptParameters.Rsa15Parameters,
                                   DecryptParameters.Rsa15Parameters);
    }

    [Fact]
    public Task EncryptDecryptWorks_RsaOaep()
    {
        return EncryptDecryptWorks("rsa-oaep",
                                   EncryptParameters.RsaOaepParameters,
                                   DecryptParameters.RsaOaepParameters);
    }

    [Fact(Skip = "Currently unsupported")]
    public Task EncryptDecryptWorks_RsaOaep256()
    {
        return EncryptDecryptWorks("rsa-oaep-256",
                                   EncryptParameters.RsaOaep256Parameters,
                                   DecryptParameters.RsaOaep256Parameters);
    }

    [Fact]
    public async Task EncryptDecryptFailsForMissingKey()
    {
        string name = Guid.NewGuid().ToString();
        string version = Guid.NewGuid().ToString("N");
        string value = Guid.NewGuid().ToString();
        byte[] rawValue = Encoding.UTF8.GetBytes(value);
        CryptographyClient cryptoClient = CreateCryptoClient(
            new Uri($"https://localhost.vault.azure.net/keys/{name}/{version}"));

        RequestFailedException ex;
        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => cryptoClient.EncryptAsync(EncryptParameters.Rsa15Parameters(rawValue)));
        Assert.Equal(404, ex.Status);

        ex = await Assert.ThrowsAsync<RequestFailedException>(
            () => cryptoClient.DecryptAsync(DecryptParameters.Rsa15Parameters(rawValue)));
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public Task WrapUnwrapWorks_Rsa15()
    {
        return WrapUnwrapWorks(KeyWrapAlgorithm.Rsa15);
    }

    [Fact]
    public Task WrapUnwrapWorks_RsaOaep()
    {
        return WrapUnwrapWorks(KeyWrapAlgorithm.RsaOaep);
    }

    [Fact(Skip = "Currently unsupported")]
    public Task WrapUnwrapWorks_RsaOaep256()
    {
        return WrapUnwrapWorks(KeyWrapAlgorithm.RsaOaep256);
    }

    [Theory]
    [InlineData("ES256")]
    [InlineData("ES384")]
    [InlineData("ES512")]
    public async Task SignVerifyWorksForEllipticCurveKey(string algName)
    {
        string name = $"sign-verify-ec-{algName}-{Guid.NewGuid()}";
        string dataToSign = $"data-to-sign-with-ec-{algName}";
        byte[] data = Encoding.UTF8.GetBytes(dataToSign);
        SignatureAlgorithm alg = new(algName);
        KeyVaultKey key = await CreateEcKeyAsync(name);
        CryptographyClient cryptoClient = CreateCryptoClient(key);

        SignResult signRes = await cryptoClient.SignDataAsync(alg, data);
        Assert.Equal(key.Key.Id, signRes.KeyId);

        VerifyResult verifyRes = await cryptoClient.VerifyDataAsync(alg, data, signRes.Signature);
        Assert.True(verifyRes.IsValid);
    }

    [Theory]
    [InlineData("RS256")]
    [InlineData("RS384")]
    [InlineData("RS512")]
    [InlineData("PS256")]
    [InlineData("PS384")]
    [InlineData("PS512")]
    public async Task SignVerifyWorksForRsaKey(string algName)
    {
        string name = $"sign-verify-rsa-{algName}-{Guid.NewGuid()}";
        string dataToSign = $"data-to-sign-with-rsa-{algName}";
        byte[] data = Encoding.UTF8.GetBytes(dataToSign);
        SignatureAlgorithm alg = new(algName);
        KeyVaultKey key = await CreateRsaKeyAsync(name);
        CryptographyClient cryptoClient = CreateCryptoClient(key);

        SignResult signRes = await cryptoClient.SignDataAsync(alg, data);
        Assert.Equal(key.Key.Id, signRes.KeyId);

        VerifyResult verifyRes = await cryptoClient.VerifyDataAsync(alg, data, signRes.Signature);
        Assert.True(verifyRes.IsValid);
    }

    private async Task EncryptDecryptWorks(
        string alg,
        Func<byte[], EncryptParameters> createEncryptParams,
        Func<byte[], DecryptParameters> createDecryptParams)
    {
        string name = $"encrypt-decrypt-{alg}-{Guid.NewGuid()}";
        string value = $"value for encrypt-decrypt-{alg}-{Guid.NewGuid()}";
        byte[] rawValue = Encoding.UTF8.GetBytes(value);
        KeyVaultKey key = await CreateRsaKeyAsync(name);
        CryptographyClient cryptoClient = CreateCryptoClient(key);

        EncryptResult encrypted = await cryptoClient
            .EncryptAsync(createEncryptParams(rawValue));
        Assert.Equal(key.Id.ToString(), encrypted.KeyId);

        DecryptResult decrypted = await cryptoClient
            .DecryptAsync(createDecryptParams(encrypted.Ciphertext));
        Assert.Equal(key.Id.ToString(), encrypted.KeyId);
        Assert.Equal(rawValue, decrypted.Plaintext);
    }

    private async Task WrapUnwrapWorks(KeyWrapAlgorithm alg)
    {
        string name = $"encrypt-decrypt-{alg}-{Guid.NewGuid()}";
        string value = $"value for encrypt-decrypt-{alg}-{Guid.NewGuid()}";
        byte[] rawValue = Encoding.UTF8.GetBytes(value);
        KeyVaultKey key = await CreateRsaKeyAsync(name);
        CryptographyClient cryptoClient = CreateCryptoClient(key);

        WrapResult wrapped = await cryptoClient
            .WrapKeyAsync(alg, rawValue);
        Assert.Equal(key.Id.ToString(), wrapped.KeyId);

        UnwrapResult unwrapped = await cryptoClient
            .UnwrapKeyAsync(alg, wrapped.EncryptedKey);
        Assert.Equal(key.Id.ToString(), wrapped.KeyId);
        Assert.Equal(rawValue, unwrapped.Key);
    }
}

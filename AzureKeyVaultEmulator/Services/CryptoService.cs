using System;
using System.Diagnostics;
using System.Security.Cryptography;
using AzureKeyVaultEmulator.Controllers;
using Microsoft.AspNetCore.WebUtilities;

namespace AzureKeyVaultEmulator.Services;

internal static class CryptoService
{
    public static KeyOperationResult Encrypt(KeyOperationsParameters op, JsonWebKey key)
    {
        if (!CanUseAlgorithm(op.Alg, key))
        {
            throw new NotSupportedException();
        }

        switch (key.Kty)
        {
            case JsonWebKeyKty.RSA:
            case JsonWebKeyKty.RSAHSM:
                return EncryptRsa(op, key);

            case JsonWebKeyKty.Oct:
            case JsonWebKeyKty.OctHSM:
            // Per https://learn.microsoft.com/en-us/azure/key-vault/keys/about-keys-details#symmetric-key-algorithms-managed-hsm-only,
            // symmetric key encryption/decryption is allowed only for HSM vaults.

            default:
                throw new NotSupportedException();
        }
    }

    public static KeyOperationResult Decrypt(KeyOperationsParameters op, JsonWebKey key)
    {
        if (!CanUseAlgorithm(op.Alg, key))
        {
            throw new NotSupportedException();
        }

        switch (key.Kty)
        {
            case JsonWebKeyKty.RSA:
            case JsonWebKeyKty.RSAHSM:
                return DecryptRsa(op, key);

            case JsonWebKeyKty.Oct:
            case JsonWebKeyKty.OctHSM:
            // Per https://learn.microsoft.com/en-us/azure/key-vault/keys/about-keys-details#symmetric-key-algorithms-managed-hsm-only,
            // symmetric key encryption/decryption is allowed only for HSM vaults.

            default:
                throw new NotSupportedException();
        }
    }

    private static bool CanUseAlgorithm(KeyOperationsParametersAlg alg, JsonWebKey key)
    {
        switch (key.Kty)
        {
            case JsonWebKeyKty.EC:
            case JsonWebKeyKty.ECHSM:
                return false;

            case JsonWebKeyKty.RSA:
            case JsonWebKeyKty.RSAHSM:
                return alg switch
                {
                    KeyOperationsParametersAlg.RSAOAEP or
                    // KeyOperationsParametersAlg.RSAOAEP256 or
                    KeyOperationsParametersAlg.RSA1_5 => true,
                    _ => false,
                };
            case JsonWebKeyKty.Oct:
            case JsonWebKeyKty.OctHSM:
                Debug.Assert(null != key.K);
                byte[] symmetricKey = WebEncoders.Base64UrlDecode(key.K);

                return alg switch
                {
                    KeyOperationsParametersAlg.A128GCM or
                    KeyOperationsParametersAlg.A128KW or
                    KeyOperationsParametersAlg.A128CBC or
                    KeyOperationsParametersAlg.A128CBCPAD => 128 == symmetricKey.Length * 8,
                    KeyOperationsParametersAlg.A192GCM or
                    KeyOperationsParametersAlg.A192KW or
                    KeyOperationsParametersAlg.A192CBC or
                    KeyOperationsParametersAlg.A192CBCPAD => 192 == symmetricKey.Length * 8,
                    KeyOperationsParametersAlg.A256GCM or
                    KeyOperationsParametersAlg.A256KW or
                    KeyOperationsParametersAlg.A256CBC or
                    KeyOperationsParametersAlg.A256CBCPAD => 256 == symmetricKey.Length * 8,
                    _ => false,
                };
            default:
                return false;
        }
    }

    private static KeyOperationResult EncryptRsa(KeyOperationsParameters op, JsonWebKey key)
    {
        byte[] value = WebEncoders.Base64UrlDecode(op.Value);

        KeyOperationResult result = op.Alg switch
        {
            KeyOperationsParametersAlg.RSAOAEP =>
                EncryptRsa(value, key, RSAEncryptionPadding.OaepSHA1),
            // KeyOperationsParametersAlg.RSAOAEP256 =>
            //     EncryptRsa(value, key, RSAEncryptionPadding.OaepSHA256),
            KeyOperationsParametersAlg.RSA1_5 =>
                EncryptRsa(value, key, RSAEncryptionPadding.Pkcs1),
            _ => throw new NotSupportedException(),
        };

        result.Kid = key.Kid;
        result.Tag = op.Tag;
        result.Aad = op.Aad;

        return result;
    }

    private static KeyOperationResult DecryptRsa(KeyOperationsParameters op, JsonWebKey key)
    {
        byte[] ciphertext = WebEncoders.Base64UrlDecode(op.Value);

        KeyOperationResult result = op.Alg switch
        {
            KeyOperationsParametersAlg.RSAOAEP =>
                DecryptRsa(ciphertext, key, RSAEncryptionPadding.OaepSHA1),
            // KeyOperationsParametersAlg.RSAOAEP256 =>
            //     DecryptRsa(ciphertext, key, RSAEncryptionPadding.OaepSHA256),
            KeyOperationsParametersAlg.RSA1_5 =>
                DecryptRsa(ciphertext, key, RSAEncryptionPadding.Pkcs1),
            _ => throw new NotSupportedException(),
        };

        result.Kid = key.Kid;
        result.Tag = op.Tag;
        result.Aad = op.Aad;

        return result;
    }

    private static KeyOperationResult EncryptRsa(
        byte[] value,
        JsonWebKey key,
        RSAEncryptionPadding padding)
    {
        (RSA rsa, RSAParameters rsaParameters) = LoadRsaFromKey(key);
        using var rsaAlg = new RSACryptoServiceProvider(rsa.KeySize);
        rsaAlg.ImportParameters(rsaParameters);
        byte[] ciphertext = rsaAlg.Encrypt(value, padding);

        return new()
        {
            Value = WebEncoders.Base64UrlEncode(ciphertext),
        };
    }

    private static KeyOperationResult DecryptRsa(
        byte[] value,
        JsonWebKey key,
        RSAEncryptionPadding padding)
    {
        (RSA rsa, RSAParameters rsaParameters) = LoadRsaFromKey(key);
        using var rsaAlg = new RSACryptoServiceProvider(rsa.KeySize);
        rsaAlg.ImportParameters(rsaParameters);
        byte[] plaintext = rsaAlg.Decrypt(value, padding);

        return new()
        {
            Value = WebEncoders.Base64UrlEncode(plaintext),
        };
    }

    private static (RSA rsa, RSAParameters rsaParameters) LoadRsaFromKey(JsonWebKey key)
    {
        RSAParameters rsaParameters = new()
        {
            D = WebEncoders.Base64UrlDecode(key.D!),
            DP = WebEncoders.Base64UrlDecode(key.Dp!),
            DQ = WebEncoders.Base64UrlDecode(key.Dq!),
            Exponent = WebEncoders.Base64UrlDecode(key.E!),
            Modulus = WebEncoders.Base64UrlDecode(key.N!),
            P = WebEncoders.Base64UrlDecode(key.P!),
            Q = WebEncoders.Base64UrlDecode(key.Q!),
            InverseQ = WebEncoders.Base64UrlDecode(key.Qi!),
        };
        RSA rsa = RSA.Create(rsaParameters);

        return (rsa, rsaParameters);
    }
}

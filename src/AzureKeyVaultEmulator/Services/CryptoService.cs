using System;
using System.Diagnostics;
using System.Security.Cryptography;
using AzureKeyVaultEmulator.Controllers;
using Microsoft.AspNetCore.WebUtilities;

#if KEYVAULT_API_7_4
using JsonWebKeyType = AzureKeyVaultEmulator.Controllers.JsonWebKeyKty;
using JsonWebKeyCurveName = AzureKeyVaultEmulator.Controllers.JsonWebKeyCrv;
using JsonWebKeyEncryptionAlgorithm = AzureKeyVaultEmulator.Controllers.KeyOperationsParametersAlg;
using JwkSignatureAlgorithm = AzureKeyVaultEmulator.Controllers.KeySignParametersAlg;
using JwkVerifyAlgorithm = AzureKeyVaultEmulator.Controllers.KeyVerifyParametersAlg;
#elif KEYVAULT_API_7_5_OR_LATER
using JwkSignatureAlgorithm = AzureKeyVaultEmulator.Controllers.JsonWebKeySignatureAlgorithm;
using JwkVerifyAlgorithm = AzureKeyVaultEmulator.Controllers.JsonWebKeySignatureAlgorithm;
#endif

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
            case JsonWebKeyType.RSA:
            case JsonWebKeyType.RSAHSM:
                return EncryptRsa(op, key);

            case JsonWebKeyType.Oct:
            case JsonWebKeyType.OctHSM:
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
            case JsonWebKeyType.RSA:
            case JsonWebKeyType.RSAHSM:
                return DecryptRsa(op, key);

            case JsonWebKeyType.Oct:
            case JsonWebKeyType.OctHSM:
            // Per https://learn.microsoft.com/en-us/azure/key-vault/keys/about-keys-details#symmetric-key-algorithms-managed-hsm-only,
            // symmetric key encryption/decryption is allowed only for HSM vaults.

            default:
                throw new NotSupportedException();
        }
    }

    public static KeyOperationResult Sign(KeySignParameters p, JsonWebKey key)
    {
        if (!CanUseAlgorithm(p.Alg, key))
        {
            throw new NotSupportedException();
        }

        switch (key.Kty)
        {
            case JsonWebKeyType.EC:
            case JsonWebKeyType.ECHSM:
                return SignEc(p, key);

            case JsonWebKeyType.RSA:
            case JsonWebKeyType.RSAHSM:
                return SignRsa(p, key);

            default:
                throw new NotSupportedException();
        }
    }

    public static KeyVerifyResult Verify(KeyVerifyParameters p, JsonWebKey key)
    {
        if (!CanUseAlgorithm((JwkSignatureAlgorithm)p.Alg, key))
        {
            throw new NotSupportedException();
        }

        switch (key.Kty)
        {
            case JsonWebKeyType.EC:
            case JsonWebKeyType.ECHSM:
                return VerifyEc(p, key);

            case JsonWebKeyType.RSA:
            case JsonWebKeyType.RSAHSM:
                return VerifyRsa(p, key);

            default:
                throw new NotSupportedException();
        }
    }

    private static bool CanUseAlgorithm(JsonWebKeyEncryptionAlgorithm alg, JsonWebKey key)
    {
        switch (key.Kty)
        {
            case JsonWebKeyType.EC:
            case JsonWebKeyType.ECHSM:
                return false;

            case JsonWebKeyType.RSA:
            case JsonWebKeyType.RSAHSM:
                return alg switch
                {
                    JsonWebKeyEncryptionAlgorithm.RSAOAEP or
                    JsonWebKeyEncryptionAlgorithm.RSAOAEP256 or
                    JsonWebKeyEncryptionAlgorithm.RSA1_5 => true,
                    _ => false,
                };
            case JsonWebKeyType.Oct:
            case JsonWebKeyType.OctHSM:
                Debug.Assert(null != key.K);
                byte[] symmetricKey = WebEncoders.Base64UrlDecode(key.K);

                return alg switch
                {
                    JsonWebKeyEncryptionAlgorithm.A128GCM or
                    JsonWebKeyEncryptionAlgorithm.A128KW or
                    JsonWebKeyEncryptionAlgorithm.A128CBC or
                    JsonWebKeyEncryptionAlgorithm.A128CBCPAD => 128 == symmetricKey.Length * 8,
                    JsonWebKeyEncryptionAlgorithm.A192GCM or
                    JsonWebKeyEncryptionAlgorithm.A192KW or
                    JsonWebKeyEncryptionAlgorithm.A192CBC or
                    JsonWebKeyEncryptionAlgorithm.A192CBCPAD => 192 == symmetricKey.Length * 8,
                    JsonWebKeyEncryptionAlgorithm.A256GCM or
                    JsonWebKeyEncryptionAlgorithm.A256KW or
                    JsonWebKeyEncryptionAlgorithm.A256CBC or
                    JsonWebKeyEncryptionAlgorithm.A256CBCPAD => 256 == symmetricKey.Length * 8,
                    _ => false,
                };
            default:
                return false;
        }
    }

    private static bool CanUseAlgorithm(JwkSignatureAlgorithm alg, JsonWebKey key)
    {
        switch (key.Kty)
        {
            case JsonWebKeyType.EC:
            case JsonWebKeyType.ECHSM:
                return alg switch
                {
                    JwkSignatureAlgorithm.ES256 or
                    JwkSignatureAlgorithm.ES256K or
                    JwkSignatureAlgorithm.ES384 or
                    JwkSignatureAlgorithm.ES512 => true,
                    _ => false,
                };

            case JsonWebKeyType.RSA:
            case JsonWebKeyType.RSAHSM:
                return alg switch
                {
                    JwkSignatureAlgorithm.PS256 or
                    JwkSignatureAlgorithm.PS384 or
                    JwkSignatureAlgorithm.PS512 or
                    JwkSignatureAlgorithm.RS256 or
                    JwkSignatureAlgorithm.RS384 or
                    JwkSignatureAlgorithm.RS512 => true,
                    // JwkSignatureAlgorithm.RSNULL => true,
                    _ => false,
                };

            case JsonWebKeyType.Oct:
            case JsonWebKeyType.OctHSM:
                return false;

            default:
                return false;
        }
    }

    private static KeyOperationResult EncryptRsa(KeyOperationsParameters op, JsonWebKey key)
    {
        byte[] value = WebEncoders.Base64UrlDecode(op.Value);

        KeyOperationResult result = op.Alg switch
        {
            JsonWebKeyEncryptionAlgorithm.RSAOAEP =>
                EncryptRsa(value, key, RSAEncryptionPadding.OaepSHA1),
            JsonWebKeyEncryptionAlgorithm.RSAOAEP256 =>
                EncryptRsa(value, key, RSAEncryptionPadding.OaepSHA256),
            JsonWebKeyEncryptionAlgorithm.RSA1_5 =>
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
            JsonWebKeyEncryptionAlgorithm.RSAOAEP =>
                DecryptRsa(ciphertext, key, RSAEncryptionPadding.OaepSHA1),
            JsonWebKeyEncryptionAlgorithm.RSAOAEP256 =>
                DecryptRsa(ciphertext, key, RSAEncryptionPadding.OaepSHA256),
            JsonWebKeyEncryptionAlgorithm.RSA1_5 =>
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
        using RSA rsa = LoadRsaFromKey(key);
        byte[] ciphertext = rsa.Encrypt(value, padding);

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
        using RSA rsa = LoadRsaFromKey(key);
        byte[] plaintext = rsa.Decrypt(value, padding);

        return new()
        {
            Value = WebEncoders.Base64UrlEncode(plaintext),
        };
    }

    private static KeyOperationResult SignEc(KeySignParameters signParams, JsonWebKey key)
    {
        (ECDsa ec, _) = LoadEcFromKey(key);
        // signParams.Value is already a digest computed by the caller, not raw message data.
        // Per Azure Key Vault's REST API: "Creates a signature from a digest using the
        // specified key." (Sign operation, KeySignParameters.value is base64url, no re-hashing
        // implied): https://learn.microsoft.com/en-us/rest/api/keyvault/keys/sign/sign
        // Alg only records which hash algorithm the caller used to produce the digest; the
        // server must sign the submitted bytes as-is. Use SignHash, not SignData (which would
        // hash the digest a second time).
        byte[] dataToSign = WebEncoders.Base64UrlDecode(signParams.Value);
        byte[] sig = ec.SignHash(dataToSign);

        return new()
        {
            Kid = key.Kid,
            Value = WebEncoders.Base64UrlEncode(sig),
        };
    }

    private static KeyVerifyResult VerifyEc(KeyVerifyParameters verifyParams, JsonWebKey key)
    {
        (ECDsa ec, _) = LoadEcFromKey(key);
        // See SignEc: verifyParams.Digest is already a digest, not raw message data; verify it
        // directly (VerifyHash), do not hash it again.
        byte[] signature = WebEncoders.Base64UrlDecode(verifyParams.Value);
        byte[] digest = WebEncoders.Base64UrlDecode(verifyParams.Digest);

        return new()
        {
            Value = ec.VerifyHash(digest, signature),
        };
    }

    private static KeyOperationResult SignRsa(KeySignParameters signParams, JsonWebKey key)
    {
        using RSA rsa = LoadRsaFromKey(key);
        (HashAlgorithmName alg, RSASignaturePadding pad) = signParams.Alg switch
        {
            JwkSignatureAlgorithm.PS256 => (HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
            JwkSignatureAlgorithm.PS384 => (HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
            JwkSignatureAlgorithm.PS512 => (HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
            JwkSignatureAlgorithm.RS256 => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            JwkSignatureAlgorithm.RS384 => (HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            JwkSignatureAlgorithm.RS512 => (HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
            _ => throw new NotSupportedException(),
        };
        // signParams.Value is already a digest, not raw message data (see SignEc for the
        // Azure Key Vault Sign REST API reference). Use SignHash (not SignData) so it is not
        // hashed a second time before signing.
        byte[] dataToSign = WebEncoders.Base64UrlDecode(signParams.Value);
        byte[] sig = rsa.SignHash(dataToSign, alg, pad);
        return new()
        {
            Kid = key.Kid,
            Value = WebEncoders.Base64UrlEncode(sig),
        };
    }

    private static KeyVerifyResult VerifyRsa(KeyVerifyParameters verifyParams, JsonWebKey key)
    {
        using RSA rsa = LoadRsaFromKey(key);
        (HashAlgorithmName alg, RSASignaturePadding pad) = verifyParams.Alg switch
        {
            JwkVerifyAlgorithm.PS256 => (HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
            JwkVerifyAlgorithm.PS384 => (HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
            JwkVerifyAlgorithm.PS512 => (HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
            JwkVerifyAlgorithm.RS256 => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            JwkVerifyAlgorithm.RS384 => (HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            JwkVerifyAlgorithm.RS512 => (HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
            _ => throw new NotSupportedException(),
        };
        // verifyParams.Value/Digest are already signature/digest bytes; use VerifyHash (not
        // VerifyData) so the digest is not hashed a second time before verification.
        byte[] signature = WebEncoders.Base64UrlDecode(verifyParams.Value);
        byte[] digest = WebEncoders.Base64UrlDecode(verifyParams.Digest);

        return new()
        {
            Value = rsa.VerifyHash(digest, signature, alg, pad),
        };
    }

    private static RSA LoadRsaFromKey(JsonWebKey key)
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

        return rsa;
    }

    private static (ECDsa, ECParameters) LoadEcFromKey(JsonWebKey key)
    {
        ECParameters ecParameters = new()
        {
            Curve = key.Crv.HasValue ? KeyFactory.GetEcCurve(key.Crv.Value) : throw new NotSupportedException(),
            Q = new()
            {
                X = WebEncoders.Base64UrlDecode(key.X!),
                Y = WebEncoders.Base64UrlDecode(key.Y!),
            },
            D = WebEncoders.Base64UrlDecode(key.D!),
        };
        ECDsa ec = ECDsa.Create(ecParameters);

        return (ec, ecParameters);
    }
}

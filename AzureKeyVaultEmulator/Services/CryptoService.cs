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

    public static KeyOperationResult Sign(KeySignParameters p, JsonWebKey key)
    {
        if (!CanUseAlgorithm(p.Alg, key))
        {
            throw new NotSupportedException();
        }

        switch (key.Kty)
        {
            case JsonWebKeyKty.EC:
            case JsonWebKeyKty.ECHSM:
                return SignEc(p, key);

            case JsonWebKeyKty.RSA:
            case JsonWebKeyKty.RSAHSM:
                return SignRsa(p, key);

            default:
                throw new NotSupportedException();
        }
    }

    public static KeyVerifyResult Verify(KeyVerifyParameters p, JsonWebKey key)
    {
        if (!CanUseAlgorithm((KeySignParametersAlg)p.Alg, key))
        {
            throw new NotSupportedException();
        }

        switch (key.Kty)
        {
            case JsonWebKeyKty.EC:
            case JsonWebKeyKty.ECHSM:
                return VerifyEc(p, key);

            case JsonWebKeyKty.RSA:
            case JsonWebKeyKty.RSAHSM:
                return VerifyRsa(p, key);

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

    private static bool CanUseAlgorithm(KeySignParametersAlg alg, JsonWebKey key)
    {
        switch (key.Kty)
        {
            case JsonWebKeyKty.EC:
            case JsonWebKeyKty.ECHSM:
                return alg switch
                {
                    KeySignParametersAlg.ES256 or
                    KeySignParametersAlg.ES256K or
                    KeySignParametersAlg.ES384 or
                    KeySignParametersAlg.ES512 => true,
                    _ => false,
                };

            case JsonWebKeyKty.RSA:
            case JsonWebKeyKty.RSAHSM:
                return alg switch
                {
                    KeySignParametersAlg.PS256 or
                    KeySignParametersAlg.PS384 or
                    KeySignParametersAlg.PS512 or
                    KeySignParametersAlg.RS256 or
                    KeySignParametersAlg.RS384 or
                    KeySignParametersAlg.RS512 => true,
                    // KeySignParametersAlg.RSNULL => true,
                    _ => false,
                };

            case JsonWebKeyKty.Oct:
            case JsonWebKeyKty.OctHSM:
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

    private static KeyOperationResult SignEc(KeySignParameters signParams, JsonWebKey key)
    {
        (ECDsa ec, _) = LoadEcFromKey(key);
        HashAlgorithmName alg = signParams.Alg switch
        {
            KeySignParametersAlg.ES256 => HashAlgorithmName.SHA256,
            KeySignParametersAlg.ES384 => HashAlgorithmName.SHA384,
            KeySignParametersAlg.ES512 => HashAlgorithmName.SHA512,
            _ => throw new NotSupportedException(),
        };
        byte[] dataToSign = WebEncoders.Base64UrlDecode(signParams.Value);
        byte[] sig = ec.SignData(dataToSign, alg);

        return new()
        {
            Kid = key.Kid,
            Value = WebEncoders.Base64UrlEncode(sig),
        };
    }

    private static KeyVerifyResult VerifyEc(KeyVerifyParameters verifyParams, JsonWebKey key)
    {
        (ECDsa ec, _) = LoadEcFromKey(key);
        HashAlgorithmName alg = verifyParams.Alg switch
        {
            KeyVerifyParametersAlg.ES256 => HashAlgorithmName.SHA256,
            KeyVerifyParametersAlg.ES384 => HashAlgorithmName.SHA384,
            KeyVerifyParametersAlg.ES512 => HashAlgorithmName.SHA512,
            _ => throw new NotSupportedException(),
        };
        byte[] signature = WebEncoders.Base64UrlDecode(verifyParams.Value);
        byte[] digest = WebEncoders.Base64UrlDecode(verifyParams.Digest);

        return new()
        {
            Value = ec.VerifyData(digest, signature, alg),
        };
    }

    private static KeyOperationResult SignRsa(KeySignParameters signParams, JsonWebKey key)
    {
        (RSA rsa, _) = LoadRsaFromKey(key);
        (HashAlgorithmName alg, RSASignaturePadding pad) = signParams.Alg switch
        {
            KeySignParametersAlg.PS256 => (HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
            KeySignParametersAlg.PS384 => (HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
            KeySignParametersAlg.PS512 => (HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
            KeySignParametersAlg.RS256 => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            KeySignParametersAlg.RS384 => (HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            KeySignParametersAlg.RS512 => (HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
            _ => throw new NotSupportedException(),
        };
        byte[] dataToSign = WebEncoders.Base64UrlDecode(signParams.Value);
        byte[] sig = rsa.SignData(dataToSign, alg, pad);
        return new()
        {
            Kid = key.Kid,
            Value = WebEncoders.Base64UrlEncode(sig),
        };
    }

    private static KeyVerifyResult VerifyRsa(KeyVerifyParameters verifyParams, JsonWebKey key)
    {
        (RSA rsa, _) = LoadRsaFromKey(key);
        (HashAlgorithmName alg, RSASignaturePadding pad) = verifyParams.Alg switch
        {
            KeyVerifyParametersAlg.PS256 => (HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
            KeyVerifyParametersAlg.PS384 => (HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
            KeyVerifyParametersAlg.PS512 => (HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
            KeyVerifyParametersAlg.RS256 => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            KeyVerifyParametersAlg.RS384 => (HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            KeyVerifyParametersAlg.RS512 => (HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
            _ => throw new NotSupportedException(),
        };
        byte[] signature = WebEncoders.Base64UrlDecode(verifyParams.Value);
        byte[] digest = WebEncoders.Base64UrlDecode(verifyParams.Digest);

        return new()
        {
            Value = rsa.VerifyData(digest, signature, alg, pad),
        };
    }

    private static (RSA, RSAParameters) LoadRsaFromKey(JsonWebKey key)
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

    private static (ECDsa, ECParameters) LoadEcFromKey(JsonWebKey key)
    {
        ECParameters ecParameters = new()
        {
            Curve = key.Crv switch
            {
                JsonWebKeyCrv.P256 => ECCurve.NamedCurves.nistP256,
                JsonWebKeyCrv.P384 => ECCurve.NamedCurves.nistP384,
                JsonWebKeyCrv.P521 => ECCurve.NamedCurves.nistP521,
                _ => throw new NotSupportedException(),
            },
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

using System;
using System.Security.Cryptography;
using AzureKeyVaultEmulator.Controllers;

#if KEYVAULT_API_7_4
using JsonWebKeyCurveName = AzureKeyVaultEmulator.Controllers.JsonWebKeyCrv;
#endif

namespace AzureKeyVaultEmulator.Services;

internal static class KeyFactory
{
    private const JsonWebKeyCurveName DefaultEcCurve = JsonWebKeyCurveName.P256;
    private const int DefaultRsaKeySize = 2048;
    private const int DefaultAesKeySize = 256;
    private const string Secp256k1Oid = "1.3.132.0.10";

    private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

    public static ECCurve GetEcCurve(JsonWebKeyCurveName crv) => crv switch
    {
        JsonWebKeyCurveName.P256 => ECCurve.NamedCurves.nistP256,
        JsonWebKeyCurveName.P384 => ECCurve.NamedCurves.nistP384,
        JsonWebKeyCurveName.P521 => ECCurve.NamedCurves.nistP521,
        JsonWebKeyCurveName.P256K => ECCurve.CreateFromValue(Secp256k1Oid),
        _ => throw new NotSupportedException(),
    };

    public static (ECDsa, JsonWebKeyCurveName) CreateEcKey(JsonWebKeyCurveName? crv)
    {
        JsonWebKeyCurveName curveName = crv ?? DefaultEcCurve;
        return (ECDsa.Create(GetEcCurve(curveName)), curveName);
    }

    public static RSA CreateRsaKey(int? keySize)
    {
        return RSA.Create(keySize ?? DefaultRsaKeySize);
    }

    public static byte[] CreateAesKey(int? keySize)
    {
        int numBits = keySize ?? DefaultAesKeySize;
        int numBytes = numBits / 8;
        if (numBytes > 32)
        {
            throw new NotSupportedException();
        }

        Span<byte> key = stackalloc byte[numBytes];
        rng.GetBytes(key);

        return key.ToArray();
    }
}

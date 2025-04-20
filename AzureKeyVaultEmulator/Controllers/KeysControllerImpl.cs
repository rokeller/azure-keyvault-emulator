using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.Converters;
using AzureKeyVaultEmulator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AzureKeyVaultEmulator.Controllers;

internal sealed class KeysControllerImpl(
    IStore<KeyBundle> store,
    IEnumToStringConvertible<Key_ops> keyOpsConverter1,
    IEnumToStringConvertible<key_ops> keyOpsConverter2,
    IHttpContextAccessor httpContextAccessor) : IKeysController
{
    private readonly IStore<KeyBundle> store = store;
    private readonly IEnumToStringConvertible<Key_ops> keyOpsConverter1 = keyOpsConverter1;
    private readonly IEnumToStringConvertible<key_ops> keyOpsConverter2 = keyOpsConverter2;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public Task<ActionResult<BackupKeyResult>> BackupKeyAsync(
        string key_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public async Task<ActionResult<KeyBundle>> CreateKeyAsync(
        string key_name,
        string api_version,
        KeyCreateParameters body,
        CancellationToken cancellationToken = default)
    {
        string version = Guid.NewGuid().ToString("N");
        Uri id = GetKeyUrl(key_name, version);
        JsonWebKey jwk = GenerateKey(id, body);
        KeyAttributes attributes = Update(body.Attributes);
        KeyBundle key = new()
        {
            Key = Update(jwk, body.Key_ops),
            Attributes = attributes,
            Tags = body.Tags,
            Managed = false,
            Release_policy = body.Release_policy,
        };

        Task storeVersion = store.StoreObjectAsync(key_name, version, key, cancellationToken);
        Task storeLatest = store.StoreObjectAsync(key_name, null, key, cancellationToken);

        await storeVersion.ConfigureAwait(ConfigureAwaitOptions.None);
        await storeLatest.ConfigureAwait(ConfigureAwaitOptions.None);

        return key;
    }

    public async Task<ActionResult<DeletedKeyBundle>> DeleteKeyAsync(
        string key_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        await store.DeleteObjectAsync(key_name, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        int now = DateTimeOffset.UtcNow.ToUnixSeconds();
        DeletedKeyBundle result = new()
        {
            RecoveryId = null,
            ScheduledPurgeDate = now,
            DeletedDate = now,
            // TODO: fill more details on deleted secret
        };

        return result;
    }

    public async Task<ActionResult<KeyBundle>> GetKeyAsync(
        string key_name,
        string key_version,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        KeyBundle? bundle = await GetKeyFromStoreAsync(key_name, key_version, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (null == bundle)
        {
            return new NotFoundResult();
        }

        return bundle;
    }

    public Task<ActionResult<KeyRotationPolicy>> GetKeyRotationPolicyAsync(
        string key_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public async Task<ActionResult<KeyListResult>> GetKeysAsync(
        int? maxresults,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        // TODO: implement paging
        List<KeyBundle> keys = await store.ListObjectsAsync(cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return await ListKeysAsync(keys, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public async Task<ActionResult<KeyListResult>> GetKeyVersionsAsync(
        string key_name,
        int? maxresults,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        // TODO: implement paging
        List<KeyBundle>? keys = await store
            .ListObjectVersionsAsync(key_name, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (null == keys)
        {
            return new NotFoundResult();
        }

        return await ListKeysAsync(keys, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public Task<ActionResult<KeyBundle>> ImportKeyAsync(
        string key_name,
        string api_version,
        KeyImportParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyReleaseResult>> ReleaseAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyReleaseParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyBundle>> RestoreKeyAsync(
        string api_version,
        KeyRestoreParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyBundle>> RotateKeyAsync(
        string key_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public async Task<ActionResult<KeyBundle>> UpdateKeyAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyUpdateParameters body,
        CancellationToken cancellationToken = default)
    {
        KeyBundle? bundle = await GetKeyFromStoreAsync(key_name, key_version, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (null == bundle)
        {
            return new NotFoundResult();
        }

        Update(bundle.Key!, body.Key_ops);
        bundle.Attributes = Update(body.Attributes, bundle.Attributes);
        bundle.Tags = body.Tags ?? bundle.Tags;
        bundle.Release_policy = body.Release_policy ?? bundle.Release_policy;

        await store.StoreObjectAsync(key_name, key_version, bundle, cancellationToken);

        return bundle;
    }

    public Task<ActionResult<KeyRotationPolicy>> UpdateKeyRotationPolicyAsync(
        string key_name,
        string api_version,
        KeyRotationPolicy body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    /*

    CRYPTO OPERATIONS

    See also
    https://learn.microsoft.com/en-us/azure/key-vault/keys/about-keys-details

    */

    public async Task<ActionResult<KeyOperationResult>> EncryptAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyOperationsParameters body,
        CancellationToken cancellationToken = default)
    {
        KeyBundle? bundle = await GetKeyFromStoreAsync(key_name, key_version, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (null == bundle || null == bundle.Key)
        {
            return new NotFoundResult();
        }

        return CryptoService.Encrypt(body, bundle.Key);
    }

    public async Task<ActionResult<KeyOperationResult>> DecryptAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyOperationsParameters body,
        CancellationToken cancellationToken = default)
    {
        KeyBundle? bundle = await GetKeyFromStoreAsync(key_name, key_version, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (null == bundle || null == bundle.Key)
        {
            return new NotFoundResult();
        }

        return CryptoService.Decrypt(body, bundle.Key);
    }

    public Task<ActionResult<KeyOperationResult>> WrapKeyAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyOperationsParameters body,
        CancellationToken cancellationToken = default)
    {
        return EncryptAsync(key_name, key_version, api_version, body, cancellationToken);
    }

    public Task<ActionResult<KeyOperationResult>> UnwrapKeyAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyOperationsParameters body,
        CancellationToken cancellationToken = default)
    {
        return DecryptAsync(key_name, key_version, api_version, body, cancellationToken);
    }

    public Task<ActionResult<KeyOperationResult>> SignAsync(
        string key_name,
        string key_version,
        string api_version,
        KeySignParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyVerifyResult>> VerifyAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyVerifyParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    private Uri GetKeyUrl(string key_name, string key_version)
    {
        Debug.Assert(null != httpContextAccessor.HttpContext,
            "The HttpContext must not be null.");

        UriBuilder keyUrl = new()
        {
            Scheme = httpContextAccessor.HttpContext.Request.Scheme,
            Host = httpContextAccessor.HttpContext.Request.Host.Host,
            Port = httpContextAccessor.HttpContext.Request.Host.Port ?? -1,
            Path = $"keys/{key_name}/{key_version}"
        };

        return keyUrl.Uri;
    }

    private async Task<KeyBundle?> GetKeyFromStoreAsync(
        string name,
        string version,
        CancellationToken cancellationToken)
    {
        KeyBundle? bundle = await store
            .ReadObjectAsync(name, version, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return bundle;
    }

    private static JsonWebKey GenerateKey(Uri id, KeyCreateParameters keyParams)
    {
        JsonWebKey key = new()
        {
            Kid = id.ToString(),
            Kty = (JsonWebKeyKty)(int)keyParams.Kty,
            Key_ops = keyParams.Key_ops?.Select(o => o.ToString()).ToList(),
        };

        switch (keyParams.Kty)
        {
            case KeyCreateParametersKty.EC:
                key.Crv = (JsonWebKeyCrv?)(int?)keyParams.Crv;
                CreateEcKeyAndPopulateJwk(key);
                break;
            case KeyCreateParametersKty.RSA:
                CreateRsaKeyAndPopulateJwk(keyParams.Key_size, key);
                break;
            case KeyCreateParametersKty.Oct:
                CreateAesKeyAndPopulateJwk(keyParams.Key_size, key);
                break;

            case KeyCreateParametersKty.ECHSM:
            case KeyCreateParametersKty.RSAHSM:
            case KeyCreateParametersKty.OctHSM:
            default:
                throw new NotSupportedException();
        }

        return key;
    }

    private static void CreateEcKeyAndPopulateJwk(JsonWebKey key)
    {
        (ECDsa ecKey, JsonWebKeyCrv crv) = KeyFactory.CreateEcKey(key.Crv);
        ECParameters ecParams = ecKey.ExportParameters(includePrivateParameters: true);

        Debug.Assert(null != ecParams.Q.X);
        Debug.Assert(null != ecParams.Q.Y);
        Debug.Assert(null != ecParams.D);

        key.Crv = crv;
        key.X = WebEncoders.Base64UrlEncode(ecParams.Q.X);
        key.Y = WebEncoders.Base64UrlEncode(ecParams.Q.Y);
        key.D = WebEncoders.Base64UrlEncode(ecParams.D);
    }

    private static void CreateRsaKeyAndPopulateJwk(int? keySize, JsonWebKey key)
    {
        RSA rsaKey = KeyFactory.CreateRsaKey(keySize);
        RSAParameters rsaParams = rsaKey.ExportParameters(includePrivateParameters: true);

        Debug.Assert(null != rsaParams.D);
        Debug.Assert(null != rsaParams.DP);
        Debug.Assert(null != rsaParams.DQ);
        Debug.Assert(null != rsaParams.Exponent);
        Debug.Assert(null != rsaParams.Modulus);
        Debug.Assert(null != rsaParams.P);
        Debug.Assert(null != rsaParams.Q);
        Debug.Assert(null != rsaParams.InverseQ);

        key.D = WebEncoders.Base64UrlEncode(rsaParams.D);
        key.Dp = WebEncoders.Base64UrlEncode(rsaParams.DP);
        key.Dq = WebEncoders.Base64UrlEncode(rsaParams.DQ);
        key.E = WebEncoders.Base64UrlEncode(rsaParams.Exponent);
        key.N = WebEncoders.Base64UrlEncode(rsaParams.Modulus);
        key.P = WebEncoders.Base64UrlEncode(rsaParams.P);
        key.Q = WebEncoders.Base64UrlEncode(rsaParams.Q);
        key.Qi = WebEncoders.Base64UrlEncode(rsaParams.InverseQ);
    }

    private static void CreateAesKeyAndPopulateJwk(int? keySize, JsonWebKey key)
    {
        byte[] aesKey = KeyFactory.CreateAesKey(keySize);

        key.K = WebEncoders.Base64UrlEncode(aesKey);
    }

    private static KeyAttributes Update(
        KeyAttributes? newest,
        KeyAttributes? oldest = null)
    {
        KeyAttributes newAttrs = newest ?? oldest ?? new();

        int now = DateTimeOffset.UtcNow.ToUnixSeconds();
        newAttrs.Created = oldest?.Created ?? now;
        newAttrs.Updated = now;

        return newAttrs;
    }

    private JsonWebKey Update(JsonWebKey key, List<Key_ops>? keyOps)
    {
        if (null != keyOps)
        {
            key.Key_ops = [.. keyOps.Select(op => keyOpsConverter1.ToString(op))];
        }

        return key;
    }

    private JsonWebKey Update(JsonWebKey key, List<key_ops>? keyOps)
    {
        if (null != keyOps)
        {
            key.Key_ops = [.. keyOps.Select(op => keyOpsConverter2.ToString(op))];
        }

        return key;
    }

    private async static Task<ActionResult<KeyListResult>> ListKeysAsync(
        List<KeyBundle> keys,
        CancellationToken cancellationToken = default)
    {
        // TODO: implement paging
        KeyItem[] values = new KeyItem[keys.Count];
        await Parallel.ForAsync(0, keys.Count, cancellationToken, (index, cancellationToken) =>
        {
            values[index] = new()
            {
                Kid = keys[index].Key!.Kid,
                Attributes = keys[index].Attributes,
                Tags = keys[index].Tags,
                Managed = keys[index].Managed,
            };
            return ValueTask.CompletedTask;
        });

        return new KeyListResult()
        {
            Value = values.ToList(),
        };
    }
}

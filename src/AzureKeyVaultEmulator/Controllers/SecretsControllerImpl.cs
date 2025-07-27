using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AzureKeyVaultEmulator.Controllers;

internal sealed partial class SecretsControllerImpl(
    IStore<SecretBundle> store,
    IHttpContextAccessor httpContextAccessor) : ISecretsController
{
    private readonly IStore<SecretBundle> store = store;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    private static readonly JsonSerializerOptions SkipNull = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<ActionResult<SecretBundle>> SetSecretAsync(
        string secret_name,
        string api_version,
        SecretSetParameters body,
        CancellationToken cancellationToken)
    {
        string version = Guid.NewGuid().ToString("N");
        Uri id = GetSecretUrl(secret_name, version);

        SecretBundle secret = new()
        {
            Id = id.ToString(),
            Value = body.Value,
            ContentType = body.ContentType,
            Attributes = Update(body.Attributes),
            Tags = body.Tags,
        };

        await store.StoreObjectAsync(secret_name,
                                     version,
                                     isLatestVersion: true,
                                     secret,
                                     cancellationToken);

        return secret;
    }

    public async Task<ActionResult<DeletedSecretBundle>> DeleteSecretAsync(
        string secret_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        await store.DeleteObjectAsync(secret_name, cancellationToken);

        int now = DateTimeOffset.UtcNow.ToUnixSeconds();
        DeletedSecretBundle result = new()
        {
            RecoveryId = null,
            ScheduledPurgeDate = now,
            DeletedDate = now,
            // TODO: fill more details on deleted secret
        };

        return result;
    }

    public async Task<ActionResult<SecretBundle>> GetSecretAsync(
        string secret_name,
        string secret_version,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        SecretBundle? bundle = await store
            .ReadObjectAsync(secret_name, secret_version, cancellationToken);

        if (null == bundle)
        {
            return new NotFoundResult();
        }

        return bundle;
    }

    public async Task<ActionResult<SecretBundle>> UpdateSecretAsync(
        string secret_name,
        string secret_version,
        string api_version,
        SecretUpdateParameters body,
        CancellationToken cancellationToken = default)
    {
        SecretBundle? bundle = await store
            .ReadObjectAsync(secret_name, secret_version, cancellationToken);

        if (null == bundle)
        {
            return new NotFoundResult();
        }

        bundle.ContentType = body.ContentType ?? bundle.ContentType;
        bundle.Attributes = Update(body.Attributes, bundle.Attributes);
        bundle.Tags = body.Tags ?? bundle.Tags;

        await store.StoreObjectAsync(secret_name,
                                     secret_version,
                                     isLatestVersion: false,
                                     bundle,
                                     cancellationToken);

        return bundle;
    }

    public async Task<ActionResult<SecretListResult>> GetSecretsAsync(
        int? maxresults,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        // TODO: implement paging
        List<SecretBundle> secrets = await store.ListObjectsAsync(cancellationToken);

        return await ListSecretsAsync(secrets, cancellationToken);
    }

    public async Task<ActionResult<SecretListResult>> GetSecretVersionsAsync(
        string secret_name,
        int? maxresults,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        List<SecretBundle>? secrets = await store
            .ListObjectVersionsAsync(secret_name, cancellationToken);

        if (null == secrets)
        {
            return new NotFoundResult();
        }

        return await ListSecretsAsync(secrets, cancellationToken);
    }

    public async Task<ActionResult<BackupSecretResult>> BackupSecretAsync(
        string secret_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        List<SecretBundle>? secrets = await store
            .ListObjectVersionsAsync(secret_name, cancellationToken);

        if (null == secrets)
        {
            return new NotFoundResult();
        }

        BackedUpSecretVersions backup = new("V1", secret_name, secrets);
        byte[] rawBackup;
        using MemoryStream memstr = new();
        {
            using GZipStream zipStream = new(memstr, CompressionLevel.Optimal);
            await JsonSerializer.SerializeAsync(
                zipStream, backup, SkipNull, cancellationToken: default);
            await zipStream.FlushAsync(default);
        }
        rawBackup = memstr.ToArray();

        BackupSecretResult result = new()
        {
            Value = WebEncoders.Base64UrlEncode(rawBackup),
        };

        return result;
    }

    public async Task<ActionResult<SecretBundle>> RestoreSecretAsync(
        string api_version,
        SecretRestoreParameters body,
        CancellationToken cancellationToken = default)
    {
        byte[] rawBackup = WebEncoders.Base64UrlDecode(body.Value);
        BackedUpSecretVersions? backup;
        using MemoryStream memstr = new(rawBackup, writable: false);
        {
            using GZipStream gzipStream = new(memstr, CompressionMode.Decompress);
            backup = await JsonSerializer
               .DeserializeAsync<BackedUpSecretVersions>(
                    gzipStream, cancellationToken: cancellationToken);
        }

        if (!backup.HasValue || backup.Value.BackupVersion != "V1" ||
            String.IsNullOrWhiteSpace(backup.Value.Name) ||
            null == backup.Value.Versions || 0 >= backup.Value.Versions.Count)
        {
            return new BadRequestResult();
        }

        string secretName = backup.Value.Name;
        if (await store.ObjectExistsAsync(secretName, cancellationToken))
        {
            return new ConflictResult();
        }

        int pos = 0;
        // Sort by creation date descending such that the first secret version
        // we store is the latest.
        IEnumerable<SecretBundle> versionsByCreationDate = backup.Value.Versions
            .OrderByDescending(v => v.Attributes!.Created);
        foreach (SecretBundle versionData in versionsByCreationDate)
        {
            string version = new Uri(versionData.Id!).Segments.Last();
            await store.StoreObjectAsync(
                secretName, version, (pos++) == 0, versionData, cancellationToken);
        }

        return await GetSecretAsync(secretName, null!, api_version, cancellationToken);
    }

    private Uri GetSecretUrl(string secret_name, string secret_version)
    {
        Debug.Assert(null != httpContextAccessor.HttpContext,
            "The HttpContext must not be null.");

        UriBuilder secretUrl = new()
        {
            Scheme = httpContextAccessor.HttpContext.Request.Scheme,
            Host = httpContextAccessor.HttpContext.Request.Host.Host,
            Port = httpContextAccessor.HttpContext.Request.Host.Port ?? -1,
            Path = $"secrets/{secret_name}/{secret_version}"
        };

        return secretUrl.Uri;
    }

    private async static Task<SecretListResult> ListSecretsAsync(
        List<SecretBundle> secrets,
        CancellationToken cancellationToken = default)
    {
        // TODO: implement paging
        SecretItem[] values = new SecretItem[secrets.Count];
        ValueTask Convert(int index, CancellationToken cancellationToken)
        {
            values[index] = new()
            {
                Id = secrets[index].Id,
                Attributes = secrets[index].Attributes,
                Tags = secrets[index].Tags,
                ContentType = secrets[index].ContentType,
                Managed = secrets[index].Managed,
            };
            return ValueTask.CompletedTask;
        }
        await Parallel.ForAsync(0, secrets.Count, cancellationToken, Convert);

        return new SecretListResult()
        {
            Value = values.ToList(),
        };
    }

    private static SecretAttributes Update(
        SecretAttributes? newest,
        SecretAttributes? oldest = null)
    {
        SecretAttributes newAttrs = newest ?? oldest ?? new();

        int now = DateTimeOffset.UtcNow.ToUnixSeconds();
        newAttrs.Created = oldest?.Created ?? now;
        newAttrs.Updated = now;

        return newAttrs;
    }
}

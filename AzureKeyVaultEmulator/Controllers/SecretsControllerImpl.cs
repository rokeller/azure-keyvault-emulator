using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Controllers;

internal sealed class SecretsControllerImpl(
    IStore<SecretBundle> store,
    IHttpContextAccessor httpContextAccessor) : ISecretsController
{
    private readonly IStore<SecretBundle> store = store;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

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
                                     cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return secret;
    }

    public async Task<ActionResult<DeletedSecretBundle>> DeleteSecretAsync(
        string secret_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        await store.DeleteObjectAsync(secret_name, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

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
            .ReadObjectAsync(secret_name, secret_version, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

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
            .ReadObjectAsync(secret_name, secret_version, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

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
                                     cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return bundle;
    }

    public async Task<ActionResult<SecretListResult>> GetSecretsAsync(
        int? maxresults,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        // TODO: implement paging
        List<SecretBundle> secrets = await store.ListObjectsAsync(cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return await ListSecretsAsync(secrets, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public async Task<ActionResult<SecretListResult>> GetSecretVersionsAsync(
        string secret_name,
        int? maxresults,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        // TODO: implement paging
        List<SecretBundle>? secrets = await store
            .ListObjectVersionsAsync(secret_name, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (null == secrets)
        {
            return new NotFoundResult();
        }

        return await ListSecretsAsync(secrets, cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public Task<ActionResult<BackupSecretResult>> BackupSecretAsync(
        string secret_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<SecretBundle>> RestoreSecretAsync(
        string api_version,
        SecretRestoreParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
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

    private async static Task<ActionResult<SecretListResult>> ListSecretsAsync(
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
        await Parallel.ForAsync(0, secrets.Count, cancellationToken, Convert)
            .ConfigureAwait(ConfigureAwaitOptions.None);

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

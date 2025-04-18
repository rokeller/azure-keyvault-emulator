using System;
using System.Threading;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Controllers;

internal sealed class KeysControllerImpl(
    IStore<KeyBundle> store,
    IHttpContextAccessor httpContextAccessor) : IKeysController
{
    private readonly IStore<KeyBundle> store = store;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public Task<ActionResult<BackupKeyResult>> BackupKeyAsync(
        string key_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyBundle>> CreateKeyAsync(
        string key_name,
        string api_version,
        KeyCreateParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyOperationResult>> DecryptAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyOperationsParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<DeletedKeyBundle>> DeleteKeyAsync(
        string key_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyOperationResult>> EncryptAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyOperationsParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyBundle>> GetKeyAsync(
        string key_name,
        string key_version,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyRotationPolicy>> GetKeyRotationPolicyAsync(
        string key_name,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyListResult>> GetKeysAsync(
        int? maxresults,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyListResult>> GetKeyVersionsAsync(
        string key_name,
        int? maxresults,
        string api_version,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
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

    public Task<ActionResult<KeyOperationResult>> SignAsync(
        string key_name,
        string key_version,
        string api_version,
        KeySignParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyOperationResult>> UnwrapKeyAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyOperationsParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyBundle>> UpdateKeyAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyUpdateParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<ActionResult<KeyRotationPolicy>> UpdateKeyRotationPolicyAsync(
        string key_name,
        string api_version,
        KeyRotationPolicy body,
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

    public Task<ActionResult<KeyOperationResult>> WrapKeyAsync(
        string key_name,
        string key_version,
        string api_version,
        KeyOperationsParameters body,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}

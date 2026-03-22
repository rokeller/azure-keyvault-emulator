using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AzureKeyVaultEmulator.Controllers;

/// <summary>
/// Controller for Key Vault keys APIs.
/// </summary>
[ApiController]
partial class KeysController
{
    /// <summary>
    /// Gets the latest version of the public part of a stored key.
    /// </summary>
    /// <remarks>
    /// The get key operation is applicable to all key types. If the requested key is symmetric, then no key material is released in the response. This operation requires the keys/get permission.
    /// </remarks>
    /// <param name="key_name">The name of the key to get.</param>
    /// <param name="api_version">Client API version.</param>
    /// <returns>A key bundle containing the key and its attributes.</returns>
    [HttpGet, Route("keys/{key_name}")]
#pragma warning disable 1573 // Disable "CS1573 Parameter '...' has no matching param tag in the XML comment for ...
    public Task<ActionResult<KeyBundle>> GetKey(
        [BindRequired] string key_name,
        [FromQuery(Name = "api-version")][BindRequired] string api_version,
        CancellationToken cancellationToken)
#pragma warning restore 1573
    {
#if KEYVAULT_API_7_4
        return _implementation.GetKeyAsync(key_name, null!, api_version, cancellationToken);
#elif KEYVAULT_API_7_5_OR_LATER
        return _implementation.GetKeyAsync(api_version, key_name, null!, cancellationToken);
#endif
    }

#if KEYVAULT_API_7_6_OR_LATER
    /// <summary>
    /// Gets the public part of a stored key along with its attestation blob.
    /// </summary>
    /// <remarks>
    /// The get key attestation operation returns the key along with its attestation blob. This operation requires the keys/get permission.
    /// </remarks>
    /// <param name="api_version">The API version to use for this operation.</param>
    /// <param name="key_name">The name of the key to retrieve attestation for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>The request has succeeded.</returns>
    [HttpGet, Route("keys/{key_name}/attestation")]
    public Task<ActionResult<KeyBundle>> GetKeyAttestation(
        [FromQuery(Name = "api-version")][BindRequired] string api_version,
        [BindRequired] string key_name,
        CancellationToken cancellationToken)
    {
        return _implementation.GetKeyAttestationAsync(api_version, key_name, null!, cancellationToken);
    }
#endif
}

/// <summary>
/// Controller for Key Vault secrets APIs.
/// </summary>
[ApiController]
partial class SecretsController
{
    /// <summary>
    /// Get the latest version of a specified secret from a given key vault.
    /// </summary>
    /// <remarks>
    /// The GET operation is applicable to any secret stored in Azure Key Vault. This operation requires the secrets/get permission.
    /// </remarks>
    /// <param name="secret_name">The name of the secret.</param>
    /// <param name="api_version">Client API version.</param>
    /// <returns>The retrieved secret.</returns>
    [HttpGet, Route("secrets/{secret_name}")]
#pragma warning disable 1573 // Disable "CS1573 Parameter '...' has no matching param tag in the XML comment for ...
    public Task<ActionResult<SecretBundle>> GetSecret(
        [BindRequired] string secret_name,
        [FromQuery(Name = "api-version")][BindRequired] string api_version,
#if KEYVAULT_API_20250701_OR_LATER
        [FromQuery] OutContentType? outContentType,
#endif
        CancellationToken cancellationToken)
#pragma warning restore 1573
    {
#if KEYVAULT_API_7_4
        return _implementation.GetSecretAsync(secret_name, null!, api_version, cancellationToken);
#elif KEYVAULT_API_20250701_OR_LATER
        return _implementation.GetSecretAsync(api_version, secret_name, null!, outContentType, cancellationToken);
#elif KEYVAULT_API_7_5_OR_LATER
        return _implementation.GetSecretAsync(api_version, secret_name, null!, cancellationToken);
#endif
    }
}

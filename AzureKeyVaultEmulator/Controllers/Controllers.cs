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
        System.Threading.CancellationToken cancellationToken)
#pragma warning restore 1573
    {
        return _implementation.GetKeyAsync(key_name, null!, api_version, cancellationToken);
    }
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
        System.Threading.CancellationToken cancellationToken)
#pragma warning restore 1573
    {
        return _implementation.GetSecretAsync(secret_name, null!, api_version, cancellationToken);
    }
}

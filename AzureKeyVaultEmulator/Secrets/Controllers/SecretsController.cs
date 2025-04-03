using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AzureKeyVaultEmulator.Models;
using AzureKeyVaultEmulator.Secrets.Models;
using AzureKeyVaultEmulator.Secrets.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Secrets.Controllers;

[ApiController]
[Route("secrets/{name}")]
[Authorize]
public class SecretsController : ControllerBase
{
    private readonly IKeyVaultSecretService secretService;

    public SecretsController(IKeyVaultSecretService secretService)
    {
        this.secretService = secretService;
    }

    [HttpPut]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SecretResponse), StatusCodes.Status200OK)]
    public IActionResult SetSecret(
        [RegularExpression("^[a-zA-Z0-9-]+$")][FromRoute] string name,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromBody] SetSecretModel requestBody)
    {
        int now = Convert.ToInt32((DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalSeconds);
        var attrs = new SecretAttributesModel()
        {
            Created = requestBody.Attributes.Created ?? now,
            Updated = requestBody.Attributes.Updated ?? now,
            Enabled = requestBody.Attributes.Enabled ?? true,
            Expiration = requestBody.Attributes.Expiration ?? null,
            NotBefore = requestBody.Attributes.NotBefore ?? null,
        };
        var secret = requestBody with
        {
            Attributes = attrs
        };
        return Ok(secretService.SetSecret(name, secret));
    }

    [HttpPatch("{version}")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SecretResponse), StatusCodes.Status200OK)]
    public IActionResult UpdateSecret(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromBody] UpdateSecretModel requestBody)
    {
        int now = Convert.ToInt32((DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalSeconds);
        var secret = secretService.Get(name, version);
        if (!secret.HasValue)
        {
            return NotFound();
        }

        var attrs = new SecretAttributesModel()
        {
            Created = secret.Value.Attributes.Created ?? requestBody.Attributes.Created ?? now,
            Updated = requestBody.Attributes.Updated ?? now,
            Enabled = requestBody.Attributes.Enabled ?? true,
            Expiration = requestBody.Attributes.Expiration ?? null,
            NotBefore = requestBody.Attributes.NotBefore ?? null,
        };

        var newSecret = secret.Value with
        {
            ContentType = requestBody.ContentType ?? secret.Value.ContentType,
            Attributes = attrs,
            Tags = requestBody.Tags ?? secret.Value.Tags,
        };

        return Ok(secretService.UpdateSecret(name, version, newSecret));
    }

    [HttpGet("{version}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SecretResponse), StatusCodes.Status200OK)]
    public IActionResult GetSecret(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromQuery(Name = "api-version")] string apiVersion)
    {
        var secretResult = secretService.Get(name, version);
        if (!secretResult.HasValue)
        {
            return NotFound();
        }

        return Ok(secretResult.Value);
    }

    [HttpGet()]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SecretResponse), StatusCodes.Status200OK)]
    public IActionResult GetSecret(
        [FromRoute] string name,
        [FromQuery(Name = "api-version")] string apiVersion)
    {
        var latestSecret = secretService.Get(name)
            .Select(_ => (SecretResponse?)_)
            .OrderByDescending(s => s.Value.Attributes.Created)
            .FirstOrDefault();
        if (!latestSecret.HasValue)
        {
            return NotFound();
        }

        return Ok(latestSecret.Value);
    }

    [HttpGet("versions")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ItemListResult<SecretResponse>), StatusCodes.Status200OK)]
    public IActionResult GetSecretVersions(
        [FromRoute] string name,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromQuery(Name = "maxresults"), Range(1, 25)] int? maxResults)
    {
        // TODO: implement paging
        IEnumerable<SecretResponse> versions = secretService.Get(name);

        return Ok(new ItemListResult<SecretResponse>(
            [.. versions.Select(StripSecretValue)], null));
    }

    private static SecretResponse StripSecretValue(SecretResponse secret)
    {
        return secret with
        {
            Value = null,
        };
    }
}

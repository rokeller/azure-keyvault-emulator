using System.ComponentModel.DataAnnotations;
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
    private readonly IKeyVaultSecretService keyVaultSecretService;

    public SecretsController(IKeyVaultSecretService keyVaultSecretService)
    {
        this.keyVaultSecretService = keyVaultSecretService;
    }

    [HttpPut]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SecretResponse), StatusCodes.Status200OK)]
    public IActionResult SetSecret(
        [RegularExpression("[a-zA-Z0-9-]+")][FromRoute] string name,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromBody] SetSecretModel requestBody)
    {
        var secret = keyVaultSecretService.SetSecret(name, requestBody);

        return Ok(secret);
    }

    [HttpGet("{version}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SecretResponse), StatusCodes.Status200OK)]
    public IActionResult GetSecret(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromQuery(Name = "api-version")] string apiVersion)
    {
        var secretResult = keyVaultSecretService.Get(name, version);

        if (secretResult == null) return NotFound();

        return Ok(secretResult);
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SecretResponse), StatusCodes.Status200OK)]
    public IActionResult GetSecret(
        [FromRoute] string name,
        [FromQuery(Name = "api-version")] string apiVersion)
    {
        var secretResult = keyVaultSecretService.Get(name);

        if (secretResult == null) return NotFound();

        return Ok(secretResult);
    }
}

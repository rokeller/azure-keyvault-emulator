using System.ComponentModel.DataAnnotations;
using AzureKeyVaultEmulator.Keys.Models;
using AzureKeyVaultEmulator.Keys.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Keys.Controllers;

[ApiController]
[Route("keys/{name}")]
[Authorize]
public sealed class KeysController : ControllerBase
{
    private readonly IKeyVaultKeyService _keyVaultKeyService;

    public KeysController(IKeyVaultKeyService keyVaultKeyService)
    {
        _keyVaultKeyService = keyVaultKeyService;
    }

    [HttpPost("create")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
    public IActionResult CreateKey(
        [RegularExpression("[a-zA-Z0-9-]+")][FromRoute] string name,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromBody] CreateKeyModel requestBody)
    {
        var createdKey = _keyVaultKeyService.CreateKey(name, requestBody);

        return Ok(createdKey);
    }

    [HttpGet("{version}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
    public IActionResult GetKey(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromQuery(Name = "api-version")] string apiVersion)
    {
        var keyResult = _keyVaultKeyService.Get(name, version);

        if (keyResult == null) return NotFound();

        return Ok(keyResult);
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
    public IActionResult GetKey(
        [FromRoute] string name,
        [FromQuery(Name = "api-version")] string apiVersion)
    {
        var keyResult = _keyVaultKeyService.Get(name);

        if (keyResult == null) return NotFound();

        return Ok(keyResult);
    }

    [HttpPost("{version}/wrapkey")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(KeyOperationResult), StatusCodes.Status200OK)]
    public IActionResult WrapKey(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromBody] KeyOperationParameters requestBody)
    {
        KeyOperationResult wrapped = _keyVaultKeyService.WrapKey(name, version, requestBody);
        if (wrapped == null)
        {
            return NotFound();
        }

        return Ok(wrapped);
    }

    [HttpPost("{version}/unwrapkey")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(KeyOperationResult), StatusCodes.Status200OK)]
    public IActionResult UnwrapKey(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromBody] KeyOperationParameters requestBody)
    {
        KeyOperationResult unwrapped = _keyVaultKeyService.UnwrapKey(name, version, requestBody);
        if (unwrapped == null)
        {
            return NotFound();
        }

        return Ok(unwrapped);
    }

    [HttpPost("{version}/encrypt")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(KeyOperationResult), StatusCodes.Status200OK)]
    public IActionResult Encrypt(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromBody] KeyOperationParameters keyOperationParameters)
    {
        var result = _keyVaultKeyService.Encrypt(name, version, keyOperationParameters);
        if (null == result)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost("{version}/decrypt")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(KeyOperationResult), StatusCodes.Status200OK)]
    public IActionResult Decrypt(
        [FromRoute] string name,
        [FromRoute] string version,
        [FromQuery(Name = "api-version")] string apiVersion,
        [FromBody] KeyOperationParameters keyOperationParameters)
    {
        var result = _keyVaultKeyService.Decrypt(name, version, keyOperationParameters);
        if (null == result)
        {
            return NotFound();
        }

        return Ok(result);
    }
}

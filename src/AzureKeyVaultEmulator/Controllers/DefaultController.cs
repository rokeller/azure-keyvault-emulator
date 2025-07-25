using System.IO;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace AzureKeyVaultEmulator.Controllers;

/// <summary>
/// Controller for default page.
/// </summary>
[AllowAnonymous]
public sealed class DefaultController : Controller
{
    /// <summary>
    /// Default page
    /// </summary>
    /// <returns></returns>
    [HttpGet("/")]
    public ActionResult Index([FromServices] IWebHostEnvironment environment)
    {
        string path = Path.Combine(environment.ContentRootPath, "index.html");
        return PhysicalFile(path, $"{MediaTypeNames.Text.Html}; charset=utf-8");
    }
}

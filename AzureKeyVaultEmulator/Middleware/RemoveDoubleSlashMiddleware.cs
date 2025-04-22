using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AzureKeyVaultEmulator.Middleware;

internal sealed class RemoveDoubleSlashMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        PathString path = context.Request.Path;
        if (path.HasValue)
        {
            // In some cases, the Key Vault SDK uses double slashes in request
            // URLs, probably due to some bad coding. Let's remove them here, as
            // the Key Vault APIs do not actually specify any endpoints with
            // double slahes.
            context.Request.Path = path.Value.Replace("//", "/");
        }
        await next.Invoke(context);
    }
}

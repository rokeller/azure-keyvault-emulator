using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.Controllers;
using AzureKeyVaultEmulator.Converters;
using AzureKeyVaultEmulator.Middleware;
using AzureKeyVaultEmulator.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<StoreOptions>(builder.Configuration.GetSection("Store"))
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        IList<JsonConverter> converters = options.JsonSerializerOptions.Converters;
        // Add some custom converters to help with conversion of some types that
        // are otherwise difficult / impossible to convert with the code generated
        // for the OpenAPI spec.
        converters.Add(new EnumStringValueConverter());
        converters.Add(new KeyCreateParametersConverter());
        converters.Add(new KeyBundleConverter());
        converters.Add(new KeyOperationsParametersConverter());
    })
    .Services
    .AddSingleton<IEnumToStringConvertible<Key_ops>>(EnumStringValueConverter.Create<Key_ops>())
    .AddSingleton<IEnumToStringConvertible<Key_ops2>>(EnumStringValueConverter.Create<Key_ops2>())

    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new()
        {
            RequireAudience = false,
            RequireExpirationTime = false,
            RequireSignedTokens = false,
            TryAllIssuerSigningKeys = false,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            SignatureValidator = (token, _) =>
            {
                return new JsonWebToken(token);
            },
        };

        Guid tenantId = builder.Configuration.GetValue<Guid>("Auth:TenantId");
        string challengeAuthorization = $"authorization=\"https://login.microsoft.com/{tenantId}\"";

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                var requestHostSplit = context.Request.Host.ToString().Split(".", 2);
                var scope = $"https://{requestHostSplit[^1]}/.default";
                context.Response.Headers.Remove("WWW-Authenticate");
                context.Response.Headers["WWW-Authenticate"] =
                    $"Bearer {challengeAuthorization}, scope=\"{scope}\", resource=\"https://vault.azure.net\"";
                return Task.CompletedTask;
            },
        };
    }).Services

    .AddHttpContextAccessor()
    .AddScoped<IKeysController, KeysControllerImpl>()
    .AddScoped<IRNGController, RNGControllerImpl>()
    .AddScoped<ISecretsController, SecretsControllerImpl>()
    .AddSingleton(UseStoreFactory<KeyBundle>("keys"))
    .AddSingleton(UseStoreFactory<KeyRotationPolicy>("key-rotation-policies"))
    .AddSingleton(UseStoreFactory<SecretBundle>("secrets"))
    .AddSingleton(UseStoreFactory<KeyBundle>("keys"))
    .AddTransient<RemoveDoubleSlashMiddleware>()
    ;

using WebApplication app = builder.Build();

if (app.Environment.EnvironmentName == "Development")
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<RemoveDoubleSlashMiddleware>();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

await app.RunAsync();

static Func<IServiceProvider, IStore<T>> UseStoreFactory<T>(
   string objectType,
   JsonSerializerOptions? writeOptions = null,
   JsonSerializerOptions? readOptions = null)
{
    Store<T> StoreFactory(IServiceProvider services)
    {
        IOptions<StoreOptions> storeOptions = services.GetRequiredService<IOptions<StoreOptions>>();
        ILogger<Store<T>> logger = services.GetRequiredService<ILogger<Store<T>>>();
        string storageDir = Path.Combine(storeOptions.Value.BaseDir, objectType);
        return new(logger, new(storageDir), writeOptions, readOptions);
    }

    return StoreFactory;
}

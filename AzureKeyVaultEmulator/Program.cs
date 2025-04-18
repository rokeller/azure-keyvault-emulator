using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.Controllers;
using AzureKeyVaultEmulator.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<StoreOptions>(builder.Configuration.GetSection("Store"))
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .Services

    .AddEndpointsApiExplorer()
    .AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Azure Key Vault Emulator",
        });
        c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "JWT Authorization header using the Bearer scheme. " +
                "Use 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE4OTAyMzkwMjIsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjUwMDEvIn0.bHLeGTRqjJrmIJbErE-1Azs724E5ibzvrIc-UQL6pws'",
            Scheme = JwtBearerDefaults.AuthenticationScheme,
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "JWT",
                    },
                },
                Array.Empty<string>()
            }
        });
        string filePath = Path.Combine(
            AppContext.BaseDirectory, "AzureKeyVaultEmulator.xml");
        c.IncludeXmlComments(filePath, true);
    })

    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            RequireSignedTokens = false,
            ValidateIssuerSigningKey = false,
            TryAllIssuerSigningKeys = false,
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
            }
        };
    }).Services

    .AddHttpContextAccessor()
    .AddScoped<IKeysController, KeysControllerImpl>()
    .AddScoped<ISecretsController, SecretsControllerImpl>()
    .AddScoped<IRNGController, RNGControllerImpl>()
    .AddSingleton(UseStoreFactory<SecretBundle>("secrets"))
    .AddSingleton(UseStoreFactory<KeyBundle>("keys"))
    ;

using WebApplication app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI();

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
        string secretsStorageDir = Path.Combine(storeOptions.Value.BaseDir, objectType);
        return new(new(secretsStorageDir), writeOptions, readOptions);
    }

    return StoreFactory;
}

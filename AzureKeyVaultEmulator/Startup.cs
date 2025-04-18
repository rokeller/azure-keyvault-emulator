using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AzureKeyVaultEmulator.Keys.Services;
using AzureKeyVaultEmulator.Secrets.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AzureKeyVaultEmulator;

internal sealed class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .Configure<StoreOptions>(_configuration.GetSection("Store"))
            .AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            })
            .Services

            .AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Azure KeyVault Emulator",
                    Version = "v1",
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
            })

            .AddHttpContextAccessor()
            .AddSingleton<IKeyVaultKeyService, KeyVaultKeyService>()
            .AddScoped<IKeyVaultSecretService, KeyVaultSecretService>()

            .AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
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

                Guid tenantId = _configuration.GetValue<Guid>("Auth:TenantId");
                string challengeAuthorization = $"authorization=\"https://login.microsoft.com/{tenantId}\"";

                x.Events = new JwtBearerEvents
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
            })
            ;
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure KeyVault Emulator v1"));

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}

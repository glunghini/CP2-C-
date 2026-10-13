using System;
using System.Text.Json.Serialization;
using ExpenseHub.Api.Application.Admin;
using ExpenseHub.Api.Application.Auth;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseHub.Api.Configuration;

/// <summary>
/// Registro dos serviços da aplicação.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra persistência, Identity, autenticação bearer, autorização e serviços de negócio.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <param name="jwtSettings">Configurações do token.</param>
    /// <returns>A mesma coleção, para encadeamento.</returns>
    public static IServiceCollection AddExpenseHub(
        this IServiceCollection services,
        IConfiguration configuration,
        JwtSettings jwtSettings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(jwtSettings);

        string connectionString = configuration.GetConnectionString("ExpenseHub")
            ?? throw new InvalidOperationException("Configure a connection string 'ExpenseHub'.");

        services.AddDbContext<ExpenseHubDbContext>(options => options.UseSqlite(connectionString));

        services.AddIdentityCore<IdentityUser>(ConfigureIdentity)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ExpenseHubDbContext>();

        services.AddSingleton(jwtSettings);
        services.AddSingleton(TimeProvider.System);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => ConfigureJwtBearer(options, jwtSettings));

        AuthorizationPolicies.Register(services.AddAuthorizationBuilder());

        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddScoped<TokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AdminUserService>();

        return services;
    }

    private static void ConfigureIdentity(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, JwtSettings jwtSettings)
    {
        SymmetricSecurityKey signingKey = jwtSettings.SigningKey;

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AppClaimTypes.Subject,
            RoleClaimType = AppClaimTypes.Role,
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = TokenStampValidation.ValidateAsync,
        };
    }
}

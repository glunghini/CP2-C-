using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Cria o banco, as roles obrigatórias e a única conta Admin inicial. Pode ser executado várias vezes.
/// </summary>
internal static class DatabaseInitializer
{
    /// <summary>
    /// Executa a criação do banco e o seed idempotente.
    /// </summary>
    /// <param name="services">Provedor raiz de serviços.</param>
    /// <param name="configuration">Configuração com a seção <c>AdminSeed</c>.</param>
    /// <param name="logger">Logger da aplicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Uma tarefa concluída ao final do seed.</returns>
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        IServiceProvider provider = scope.ServiceProvider;

        ExpenseHubDbContext dbContext = provider.GetRequiredService<ExpenseHubDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        RoleManager<IdentityRole> roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (string role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)));
            }
        }

        IConfigurationSection adminSection = configuration.GetSection("AdminSeed");
        string? adminEmail = adminSection["Email"];
        string? adminPass = adminSection["Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPass))
        {
            logger.LogWarning("Conta Admin não criada: configure AdminSeed:Email e AdminSeed:Password fora do código-fonte.");
            return;
        }

        UserManager<IdentityUser> userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser? admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
            };

            EnsureSucceeded(await userManager.CreateAsync(admin, adminPass));
            logger.LogInformation("Conta Admin inicial criada.");
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, AppRoles.Admin));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Falha no seed do Identity: " + IdentityErrors.Describe(result));
        }
    }
}

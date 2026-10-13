using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Rejeita tokens emitidos antes da última mudança de segurança do usuário,
/// como a alteração de roles. Assim, o usuário precisa autenticar novamente.
/// </summary>
internal static class TokenStampValidation
{
    /// <summary>
    /// Valida o carimbo de segurança presente no token contra o valor persistido.
    /// </summary>
    /// <param name="context">Contexto do token já validado criptograficamente.</param>
    /// <returns>Uma tarefa concluída quando a validação termina.</returns>
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ClaimsPrincipal? principal = context.Principal;
        string? userId = principal?.FindFirst(AppClaimTypes.Subject)?.Value;
        string? stamp = principal?.FindFirst(AppClaimTypes.SecurityStamp)?.Value;
        if (string.IsNullOrEmpty(userId) || stamp is null)
        {
            context.Fail("O token não identifica um usuário válido.");
            return;
        }

        UserManager<IdentityUser> userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser? user = await userManager.FindByIdAsync(userId);
        if (user is null || !string.Equals(user.SecurityStamp ?? string.Empty, stamp, StringComparison.Ordinal))
        {
            context.Fail("A sessão expirou após uma alteração de segurança. Autentique-se novamente.");
        }
    }
}

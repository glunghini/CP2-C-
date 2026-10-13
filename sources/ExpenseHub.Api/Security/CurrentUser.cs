using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Identidade autenticada usada pelas regras de negócio.
/// Sempre derivada do token, nunca do corpo da requisição.
/// </summary>
internal sealed class CurrentUser
{
    private readonly HashSet<string> _roles;

    /// <summary>
    /// Cria a identidade a partir do identificador e das roles.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="roles">Roles do usuário.</param>
    public CurrentUser(string userId, IEnumerable<string> roles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(roles);

        UserId = userId;
        _roles = new HashSet<string>(roles, StringComparer.Ordinal);
    }

    /// <summary>
    /// Identificador do usuário autenticado.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Indica se o usuário possui a role.
    /// </summary>
    /// <param name="role">Role avaliada.</param>
    /// <returns><c>true</c> quando a role está presente.</returns>
    public bool IsInRole(string role)
    {
        return _roles.Contains(role);
    }

    /// <summary>
    /// Indica se o usuário possui ao menos uma das roles.
    /// </summary>
    /// <param name="roles">Roles avaliadas.</param>
    /// <returns><c>true</c> quando alguma role está presente.</returns>
    public bool IsInAnyRole(IEnumerable<string> roles)
    {
        return roles.Any(_roles.Contains);
    }

    /// <summary>
    /// Indica se o usuário é o proprietário informado.
    /// </summary>
    /// <param name="ownerId">Proprietário do recurso.</param>
    /// <returns><c>true</c> quando os identificadores são iguais.</returns>
    public bool Owns(string ownerId)
    {
        return string.Equals(UserId, ownerId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Constrói a identidade a partir das claims validadas pelo middleware de autenticação.
    /// </summary>
    /// <param name="principal">Usuário da requisição.</param>
    /// <param name="currentUser">Identidade construída.</param>
    /// <returns><c>true</c> quando o token possui identificador.</returns>
    public static bool TryCreate(ClaimsPrincipal principal, [NotNullWhen(true)] out CurrentUser? currentUser)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? userId = principal.FindFirst(AppClaimTypes.Subject)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            currentUser = null;
            return false;
        }

        IEnumerable<string> roles = principal.FindAll(AppClaimTypes.Role).Select(claim => claim.Value);
        currentUser = new CurrentUser(userId, roles);
        return true;
    }
}

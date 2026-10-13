using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Application.Admin;

/// <summary>
/// Regras para a alteração de roles feita pelo Admin.
/// </summary>
internal static class RoleChangeRules
{
    /// <summary>
    /// Valida e normaliza as roles solicitadas.
    /// </summary>
    /// <param name="requestedRoles">Roles enviadas pelo cliente.</param>
    /// <param name="targetIsCurrentUser">Indica se o Admin está alterando a própria conta.</param>
    /// <returns>As roles canônicas, sem repetição, ou a falha.</returns>
    public static ServiceResult<IReadOnlyList<string>> Normalize(IReadOnlyList<string> requestedRoles, bool targetIsCurrentUser)
    {
        ArgumentNullException.ThrowIfNull(requestedRoles);

        List<string> normalized = [];
        List<string> unknown = [];
        foreach (string requested in requestedRoles)
        {
            string? known = AppRoles.FindKnown(requested);
            if (known is null)
            {
                unknown.Add(requested);
                continue;
            }

            if (!normalized.Contains(known, StringComparer.Ordinal))
            {
                normalized.Add(known);
            }
        }

        if (unknown.Count > 0)
        {
            string message = "Roles desconhecidas: " + string.Join(", ", unknown)
                + ". Roles válidas: " + string.Join(", ", AppRoles.All) + ".";
            return ServiceResult<IReadOnlyList<string>>.Failure(ServiceErrorKind.Validation, message);
        }

        if (targetIsCurrentUser && !normalized.Contains(AppRoles.Admin, StringComparer.Ordinal))
        {
            return ServiceResult<IReadOnlyList<string>>.Failure(
                ServiceErrorKind.Conflict,
                "O Admin não pode remover a própria role Admin.");
        }

        normalized.Sort(StringComparer.Ordinal);
        return ServiceResult<IReadOnlyList<string>>.Success(normalized);
    }
}

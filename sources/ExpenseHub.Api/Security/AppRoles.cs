using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Roles conhecidas pela aplicação.
/// </summary>
internal static class AppRoles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string Approver = "Approver";
    public const string Finance = "Finance";
    public const string Auditor = "Auditor";

    /// <summary>
    /// Todas as roles que devem existir para o funcionamento da aplicação.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = [Admin, Employee, Approver, Finance, Auditor];

    /// <summary>
    /// Roles que concedem leitura de reembolsos.
    /// </summary>
    public static IReadOnlyList<string> ExpenseReaders { get; } = [Employee, Approver, Finance, Auditor];

    /// <summary>
    /// Procura a grafia canônica de uma role, ignorando maiúsculas e minúsculas.
    /// </summary>
    /// <param name="role">Nome informado pelo cliente.</param>
    /// <returns>O nome canônico ou <c>null</c> quando a role não existe.</returns>
    public static string? FindKnown(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        string trimmed = role.Trim();
        foreach (string known in All)
        {
            if (string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return known;
            }
        }

        return null;
    }
}

using System;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Políticas por role aplicadas nas rotas. Ownership e estado são verificados no serviço.
/// </summary>
internal static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string EmployeeOnly = "EmployeeOnly";
    public const string ApproverOnly = "ApproverOnly";
    public const string FinanceOnly = "FinanceOnly";
    public const string ExpenseReader = "ExpenseReader";

    /// <summary>
    /// Registra as políticas no builder de autorização.
    /// </summary>
    /// <param name="builder">Builder de autorização.</param>
    public static void Register(AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy(AdminOnly, policy => policy.RequireRole(AppRoles.Admin));
        builder.AddPolicy(EmployeeOnly, policy => policy.RequireRole(AppRoles.Employee));
        builder.AddPolicy(ApproverOnly, policy => policy.RequireRole(AppRoles.Approver));
        builder.AddPolicy(FinanceOnly, policy => policy.RequireRole(AppRoles.Finance));
        builder.AddPolicy(ExpenseReader, policy => policy.RequireRole(AppRoles.ExpenseReaders));
    }
}

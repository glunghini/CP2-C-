using System;
using System.Linq.Expressions;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Application.Expenses;

/// <summary>
/// Escopo de leitura de reembolsos por perfil, expresso como filtro traduzível para SQL.
/// Roles acumuladas recebem a união dos escopos.
/// </summary>
internal static class ExpenseVisibility
{
    /// <summary>
    /// Cria o filtro de leitura para o usuário.
    /// Employee vê os próprios; Approver vê Submitted; Finance vê Approved e Paid; Auditor vê todos.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <returns>Expressão aplicada na consulta antes de materializar os dados.</returns>
    public static Expression<Func<Expense, bool>> For(CurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        string userId = user.UserId;
        bool isEmployee = user.IsInRole(AppRoles.Employee);
        bool isApprover = user.IsInRole(AppRoles.Approver);
        bool isFinance = user.IsInRole(AppRoles.Finance);
        bool isAuditor = user.IsInRole(AppRoles.Auditor);

        return expense => isAuditor
            || (isEmployee && expense.OwnerId == userId)
            || (isApprover && expense.Status == ExpenseStatus.Submitted)
            || (isFinance && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid));
    }

    /// <summary>
    /// Escopo das ações de Approver e Finance. Rascunhos de outros usuários continuam privados (404);
    /// reembolsos já enviados entram no escopo para que transições repetidas ou fora de ordem retornem 409.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <returns>Expressão aplicada na consulta antes de materializar os dados.</returns>
    public static Expression<Func<Expense, bool>> ForWorkflow(CurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        string userId = user.UserId;
        return expense => expense.Status != ExpenseStatus.Draft || expense.OwnerId == userId;
    }
}

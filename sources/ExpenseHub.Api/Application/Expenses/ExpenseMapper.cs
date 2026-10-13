using System;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Responses;

namespace ExpenseHub.Api.Application.Expenses;

/// <summary>
/// Conversão de entidades em respostas da API.
/// </summary>
internal static class ExpenseMapper
{
    /// <summary>
    /// Converte um reembolso.
    /// </summary>
    /// <param name="expense">Entidade.</param>
    /// <returns>A resposta.</returns>
    public static ExpenseResponse ToResponse(Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new ExpenseResponse
        {
            Id = expense.Id,
            OwnerId = expense.OwnerId,
            Description = expense.Description,
            Amount = expense.Amount,
            ExpenseDate = expense.ExpenseDate,
            CategoryId = expense.CategoryId,
            Status = expense.Status,
            CreatedAtUtc = AsUtc(expense.CreatedAtUtc),
            UpdatedAtUtc = AsUtc(expense.UpdatedAtUtc),
            SubmittedAtUtc = AsUtc(expense.SubmittedAtUtc),
            DecidedById = expense.DecidedById,
            DecidedAtUtc = AsUtc(expense.DecidedAtUtc),
            RejectionReason = expense.RejectionReason,
            PaidById = expense.PaidById,
            PaidAtUtc = AsUtc(expense.PaidAtUtc),
        };
    }

    /// <summary>
    /// Converte um registro de histórico.
    /// </summary>
    /// <param name="history">Entidade.</param>
    /// <returns>A resposta.</returns>
    public static ExpenseHistoryResponse ToResponse(ExpenseHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return new ExpenseHistoryResponse(
            history.Id,
            history.ExpenseId,
            history.Action,
            history.ActorId,
            AsUtc(history.OccurredAtUtc),
            history.FromStatus,
            history.ToStatus,
            history.Justification,
            history.Changes);
    }

    /// <summary>
    /// Converte uma categoria.
    /// </summary>
    /// <param name="category">Entidade.</param>
    /// <returns>A resposta.</returns>
    public static ExpenseCategoryResponse ToResponse(ExpenseCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new ExpenseCategoryResponse(category.Id, category.Name);
    }

    private static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? AsUtc(DateTime? value)
    {
        return value.HasValue ? AsUtc(value.Value) : null;
    }
}

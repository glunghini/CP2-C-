using System;

namespace ExpenseHub.Api.Application.Expenses;

/// <summary>
/// Validações de domínio dos reembolsos, aplicadas no serviço além das validações declarativas dos DTOs.
/// </summary>
internal static class ExpenseRules
{
    public const int TextMinLength = 10;
    public const int TextMaxLength = 500;
    public const decimal MinimumAmount = 0.01m;
    public const decimal MaximumAmount = int.MaxValue;

    /// <summary>
    /// Remove espaços nas extremidades de um texto informado pelo cliente.
    /// </summary>
    /// <param name="value">Texto recebido.</param>
    /// <returns>O texto normalizado ou vazio.</returns>
    public static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Valida os campos de um rascunho.
    /// </summary>
    /// <param name="description">Descrição normalizada.</param>
    /// <param name="amount">Valor.</param>
    /// <param name="expenseDate">Data da despesa.</param>
    /// <param name="todayUtc">Data atual do servidor em UTC.</param>
    /// <returns>A mensagem de erro ou <c>null</c> quando os dados são válidos.</returns>
    public static string? ValidateDraft(string description, decimal amount, DateOnly expenseDate, DateOnly todayUtc)
    {
        ArgumentNullException.ThrowIfNull(description);

        if (description.Length is < TextMinLength or > TextMaxLength)
        {
            return "A descrição deve ter entre 10 e 500 caracteres.";
        }

        if (amount is < MinimumAmount or > MaximumAmount)
        {
            return "O valor deve estar entre R$ 0,01 e R$ 2.147.483.647,00.";
        }

        if (expenseDate > todayUtc)
        {
            return "A data da despesa não pode estar no futuro.";
        }

        return null;
    }

    /// <summary>
    /// Valida a justificativa de reprovação.
    /// </summary>
    /// <param name="justification">Justificativa normalizada.</param>
    /// <returns>A mensagem de erro ou <c>null</c> quando a justificativa é válida.</returns>
    public static string? ValidateJustification(string justification)
    {
        ArgumentNullException.ThrowIfNull(justification);

        return justification.Length is < TextMinLength or > TextMaxLength
            ? "A justificativa deve ter entre 10 e 500 caracteres."
            : null;
    }
}

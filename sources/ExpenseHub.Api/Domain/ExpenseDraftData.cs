using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Dados editáveis de um reembolso em Draft, já validados e normalizados.
/// </summary>
/// <param name="Description">Descrição sem espaços nas extremidades.</param>
/// <param name="Amount">Valor do reembolso.</param>
/// <param name="ExpenseDate">Data da despesa.</param>
/// <param name="CategoryId">Categoria existente.</param>
internal sealed record ExpenseDraftData(string Description, decimal Amount, DateOnly ExpenseDate, int CategoryId);

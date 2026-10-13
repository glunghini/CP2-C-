namespace ExpenseHub.Api.Responses;

/// <summary>
/// Categoria disponível para os reembolsos.
/// </summary>
/// <param name="Id">Identificador.</param>
/// <param name="Name">Nome.</param>
internal sealed record ExpenseCategoryResponse(int Id, string Name);

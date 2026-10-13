using System.Collections.Generic;

namespace ExpenseHub.Api.Responses;

/// <summary>
/// Dados públicos de um usuário.
/// </summary>
/// <param name="Id">Identificador do usuário.</param>
/// <param name="Email">E-mail do usuário.</param>
/// <param name="Roles">Roles atribuídas.</param>
internal sealed record UserResponse(string Id, string? Email, IReadOnlyList<string> Roles);

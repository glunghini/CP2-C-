using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Responses;

/// <summary>
/// Registro de histórico devolvido pela API.
/// </summary>
/// <param name="Id">Identificador do registro.</param>
/// <param name="ExpenseId">Reembolso.</param>
/// <param name="Action">Ação realizada.</param>
/// <param name="ActorId">Usuário que realizou a ação.</param>
/// <param name="OccurredAtUtc">Instante em UTC.</param>
/// <param name="FromStatus">Estado anterior.</param>
/// <param name="ToStatus">Estado posterior.</param>
/// <param name="Justification">Justificativa da reprovação.</param>
/// <param name="Changes">Campos alterados em Draft, em JSON.</param>
internal sealed record ExpenseHistoryResponse(
    Guid Id,
    Guid ExpenseId,
    ExpenseAction Action,
    string ActorId,
    DateTime OccurredAtUtc,
    ExpenseStatus? FromStatus,
    ExpenseStatus ToStatus,
    string? Justification,
    string? Changes);

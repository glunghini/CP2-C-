using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro imutável de uma ação realizada sobre um reembolso.
/// </summary>
internal sealed class ExpenseHistory
{
    private ExpenseHistory()
    {
    }

    /// <summary>
    /// Identificador do registro.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Reembolso afetado.
    /// </summary>
    public Guid ExpenseId { get; private set; }

    /// <summary>
    /// Ação realizada.
    /// </summary>
    public ExpenseAction Action { get; private set; }

    /// <summary>
    /// Usuário autenticado que realizou a ação.
    /// </summary>
    public string ActorId { get; private set; } = string.Empty;

    /// <summary>
    /// Instante da ação em UTC, definido pelo servidor.
    /// </summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>
    /// Estado anterior; nulo na criação.
    /// </summary>
    public ExpenseStatus? FromStatus { get; private set; }

    /// <summary>
    /// Estado posterior.
    /// </summary>
    public ExpenseStatus ToStatus { get; private set; }

    /// <summary>
    /// Justificativa informada na reprovação.
    /// </summary>
    public string? Justification { get; private set; }

    /// <summary>
    /// Alterações realizadas em Draft, serializadas em JSON.
    /// </summary>
    public string? Changes { get; private set; }

    /// <summary>
    /// Cria um novo registro de histórico.
    /// </summary>
    /// <param name="entry">Dados do registro.</param>
    /// <returns>O registro criado.</returns>
    public static ExpenseHistory Record(ExpenseHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ExpenseHistory
        {
            Id = Guid.NewGuid(),
            ExpenseId = entry.ExpenseId,
            Action = entry.Action,
            ActorId = entry.ActorId,
            OccurredAtUtc = entry.OccurredAtUtc,
            FromStatus = entry.FromStatus,
            ToStatus = entry.ToStatus,
            Justification = entry.Justification,
            Changes = entry.Changes,
        };
    }
}

/// <summary>
/// Dados necessários para registrar uma ação no histórico.
/// </summary>
/// <param name="ExpenseId">Reembolso afetado.</param>
/// <param name="Action">Ação realizada.</param>
/// <param name="ActorId">Usuário que realizou a ação.</param>
/// <param name="OccurredAtUtc">Instante da ação em UTC.</param>
/// <param name="FromStatus">Estado anterior.</param>
/// <param name="ToStatus">Estado posterior.</param>
internal sealed record ExpenseHistoryEntry(
    Guid ExpenseId,
    ExpenseAction Action,
    string ActorId,
    DateTime OccurredAtUtc,
    ExpenseStatus? FromStatus,
    ExpenseStatus ToStatus)
{
    /// <summary>
    /// Justificativa opcional.
    /// </summary>
    public string? Justification { get; init; }

    /// <summary>
    /// Alterações opcionais em JSON.
    /// </summary>
    public string? Changes { get; init; }
}

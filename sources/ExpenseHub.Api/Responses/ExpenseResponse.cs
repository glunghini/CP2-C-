using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Responses;

/// <summary>
/// Representação de um reembolso devolvida pela API.
/// </summary>
internal sealed record ExpenseResponse
{
    /// <summary>
    /// Identificador.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Proprietário.
    /// </summary>
    public required string OwnerId { get; init; }

    /// <summary>
    /// Descrição.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Valor.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Data da despesa.
    /// </summary>
    public required DateOnly ExpenseDate { get; init; }

    /// <summary>
    /// Categoria.
    /// </summary>
    public required int CategoryId { get; init; }

    /// <summary>
    /// Estado atual.
    /// </summary>
    public required ExpenseStatus Status { get; init; }

    /// <summary>
    /// Criação em UTC.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Última alteração em UTC.
    /// </summary>
    public required DateTime UpdatedAtUtc { get; init; }

    /// <summary>
    /// Envio em UTC.
    /// </summary>
    public DateTime? SubmittedAtUtc { get; init; }

    /// <summary>
    /// Aprovador que decidiu.
    /// </summary>
    public string? DecidedById { get; init; }

    /// <summary>
    /// Decisão em UTC.
    /// </summary>
    public DateTime? DecidedAtUtc { get; init; }

    /// <summary>
    /// Justificativa da reprovação.
    /// </summary>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// Usuário que registrou o pagamento.
    /// </summary>
    public string? PaidById { get; init; }

    /// <summary>
    /// Pagamento em UTC.
    /// </summary>
    public DateTime? PaidAtUtc { get; init; }
}

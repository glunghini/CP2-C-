using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro do pagamento simulado de um reembolso aprovado.
/// </summary>
internal sealed class PaymentRecord
{
    private PaymentRecord()
    {
    }

    /// <summary>
    /// Identificador do pagamento.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Reembolso pago.
    /// </summary>
    public Guid ExpenseId { get; private set; }

    /// <summary>
    /// Valor pago.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Usuário do financeiro que registrou o pagamento.
    /// </summary>
    public string PaidById { get; private set; } = string.Empty;

    /// <summary>
    /// Instante do pagamento em UTC, definido pelo servidor.
    /// </summary>
    public DateTime PaidAtUtc { get; private set; }

    /// <summary>
    /// Cria o registro de pagamento.
    /// </summary>
    /// <param name="expenseId">Reembolso pago.</param>
    /// <param name="amount">Valor pago.</param>
    /// <param name="paidById">Usuário que registrou o pagamento.</param>
    /// <param name="paidAtUtc">Instante do pagamento.</param>
    /// <returns>O registro criado.</returns>
    public static PaymentRecord Create(Guid expenseId, decimal amount, string paidById, DateTime paidAtUtc)
    {
        return new PaymentRecord
        {
            Id = Guid.NewGuid(),
            ExpenseId = expenseId,
            Amount = amount,
            PaidById = paidById,
            PaidAtUtc = paidAtUtc,
        };
    }
}

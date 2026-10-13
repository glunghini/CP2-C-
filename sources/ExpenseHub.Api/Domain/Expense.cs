using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Solicitação de reembolso. O estado só muda pelos métodos de transição.
/// </summary>
internal sealed class Expense
{
    private static readonly JsonSerializerOptions _changesJsonOptions = new(JsonSerializerDefaults.Web);

    private Expense()
    {
    }

    /// <summary>
    /// Identificador gerado pelo servidor.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Proprietário, obtido do usuário autenticado.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>
    /// Descrição da despesa.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Valor único do reembolso.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Data em que a despesa ocorreu.
    /// </summary>
    public DateOnly ExpenseDate { get; private set; }

    /// <summary>
    /// Categoria da despesa.
    /// </summary>
    public int CategoryId { get; private set; }

    /// <summary>
    /// Navegação para a categoria.
    /// </summary>
    public ExpenseCategory? Category { get; private set; }

    /// <summary>
    /// Estado atual, definido exclusivamente pelo servidor.
    /// </summary>
    public ExpenseStatus Status { get; private set; }

    /// <summary>
    /// Instante de criação em UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Instante da última alteração em UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Instante do envio em UTC.
    /// </summary>
    public DateTime? SubmittedAtUtc { get; private set; }

    /// <summary>
    /// Aprovador que aprovou ou reprovou.
    /// </summary>
    public string? DecidedById { get; private set; }

    /// <summary>
    /// Instante da decisão em UTC.
    /// </summary>
    public DateTime? DecidedAtUtc { get; private set; }

    /// <summary>
    /// Justificativa da reprovação.
    /// </summary>
    public string? RejectionReason { get; private set; }

    /// <summary>
    /// Usuário do financeiro que registrou o pagamento.
    /// </summary>
    public string? PaidById { get; private set; }

    /// <summary>
    /// Instante do pagamento em UTC.
    /// </summary>
    public DateTime? PaidAtUtc { get; private set; }

    /// <summary>
    /// Versão usada como token de concorrência.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Cria um reembolso em Draft e o registro de histórico correspondente.
    /// </summary>
    /// <param name="ownerId">Usuário autenticado que cria o reembolso.</param>
    /// <param name="data">Dados já validados.</param>
    /// <param name="nowUtc">Instante atual do servidor.</param>
    /// <returns>O reembolso e o histórico de criação.</returns>
    public static (Expense Draft, ExpenseHistory History) CreateDraft(string ownerId, ExpenseDraftData data, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(data);

        Expense expense = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Description = data.Description,
            Amount = data.Amount,
            ExpenseDate = data.ExpenseDate,
            CategoryId = data.CategoryId,
            Status = ExpenseStatus.Draft,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Version = 1,
        };

        ExpenseHistory history = ExpenseHistory.Record(
            new ExpenseHistoryEntry(expense.Id, ExpenseAction.Created, ownerId, nowUtc, null, ExpenseStatus.Draft));

        return (expense, history);
    }

    /// <summary>
    /// Altera os dados de um Draft e registra os campos modificados.
    /// </summary>
    /// <param name="data">Novos dados já validados.</param>
    /// <param name="actorId">Usuário que altera o reembolso.</param>
    /// <param name="nowUtc">Instante atual do servidor.</param>
    /// <returns>O histórico da alteração ou <c>null</c> quando nada mudou.</returns>
    public ExpenseHistory? UpdateDraft(ExpenseDraftData data, string actorId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureCanApply(ExpenseAction.Updated);

        Dictionary<string, FieldChange> changes = new(StringComparer.Ordinal);
        if (!string.Equals(Description, data.Description, StringComparison.Ordinal))
        {
            changes["description"] = new FieldChange(Description, data.Description);
        }

        if (Amount != data.Amount)
        {
            changes["amount"] = new FieldChange(
                Amount.ToString(CultureInfo.InvariantCulture),
                data.Amount.ToString(CultureInfo.InvariantCulture));
        }

        if (ExpenseDate != data.ExpenseDate)
        {
            changes["expenseDate"] = new FieldChange(
                ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                data.ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (CategoryId != data.CategoryId)
        {
            changes["categoryId"] = new FieldChange(
                CategoryId.ToString(CultureInfo.InvariantCulture),
                data.CategoryId.ToString(CultureInfo.InvariantCulture));
        }

        if (changes.Count == 0)
        {
            return null;
        }

        Description = data.Description;
        Amount = data.Amount;
        ExpenseDate = data.ExpenseDate;
        CategoryId = data.CategoryId;

        string serializedChanges = JsonSerializer.Serialize(changes, _changesJsonOptions);
        return Transition(ExpenseAction.Updated, actorId, nowUtc, null, serializedChanges);
    }

    /// <summary>
    /// Envia o Draft para aprovação.
    /// </summary>
    /// <param name="actorId">Proprietário que envia.</param>
    /// <param name="nowUtc">Instante atual do servidor.</param>
    /// <returns>O histórico do envio.</returns>
    public ExpenseHistory Submit(string actorId, DateTime nowUtc)
    {
        ExpenseHistory history = Transition(ExpenseAction.Submitted, actorId, nowUtc, null, null);
        SubmittedAtUtc = nowUtc;
        return history;
    }

    /// <summary>
    /// Aprova um reembolso enviado.
    /// </summary>
    /// <param name="actorId">Aprovador.</param>
    /// <param name="nowUtc">Instante atual do servidor.</param>
    /// <returns>O histórico da aprovação.</returns>
    public ExpenseHistory Approve(string actorId, DateTime nowUtc)
    {
        ExpenseHistory history = Transition(ExpenseAction.Approved, actorId, nowUtc, null, null);
        DecidedById = actorId;
        DecidedAtUtc = nowUtc;
        return history;
    }

    /// <summary>
    /// Reprova um reembolso enviado com justificativa.
    /// </summary>
    /// <param name="actorId">Aprovador.</param>
    /// <param name="justification">Justificativa já validada.</param>
    /// <param name="nowUtc">Instante atual do servidor.</param>
    /// <returns>O histórico da reprovação.</returns>
    public ExpenseHistory Reject(string actorId, string justification, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);

        ExpenseHistory history = Transition(ExpenseAction.Rejected, actorId, nowUtc, justification, null);
        DecidedById = actorId;
        DecidedAtUtc = nowUtc;
        RejectionReason = justification;
        return history;
    }

    /// <summary>
    /// Registra o pagamento de um reembolso aprovado.
    /// </summary>
    /// <param name="actorId">Usuário do financeiro.</param>
    /// <param name="nowUtc">Instante atual do servidor.</param>
    /// <returns>O histórico e o registro de pagamento.</returns>
    public (ExpenseHistory History, PaymentRecord Payment) Pay(string actorId, DateTime nowUtc)
    {
        ExpenseHistory history = Transition(ExpenseAction.Paid, actorId, nowUtc, null, null);
        PaidById = actorId;
        PaidAtUtc = nowUtc;
        PaymentRecord payment = PaymentRecord.Create(Id, Amount, actorId, nowUtc);
        return (history, payment);
    }

    private void EnsureCanApply(ExpenseAction action)
    {
        if (!ExpenseStateMachine.CanApply(Status, action))
        {
            throw new InvalidOperationException("A transição solicitada não é permitida no estado atual.");
        }
    }

    private ExpenseHistory Transition(
        ExpenseAction action,
        string actorId,
        DateTime nowUtc,
        string? justification,
        string? changes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        if (!ExpenseStateMachine.TryGetNextStatus(Status, action, out ExpenseStatus next))
        {
            throw new InvalidOperationException("A transição solicitada não é permitida no estado atual.");
        }

        ExpenseStatus previous = Status;
        Status = next;
        UpdatedAtUtc = nowUtc;
        Version++;

        return ExpenseHistory.Record(
            new ExpenseHistoryEntry(Id, action, actorId, nowUtc, previous, next)
            {
                Justification = justification,
                Changes = changes,
            });
    }

    private sealed record FieldChange(string From, string To);
}

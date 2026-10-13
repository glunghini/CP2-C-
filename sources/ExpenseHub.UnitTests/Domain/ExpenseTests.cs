using System;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Testes do comportamento da entidade de reembolso.
/// </summary>
[TestClass]
public sealed class ExpenseTests
{
    private static readonly DateTime _now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ExpenseDraftData _data = new("Hospedagem em viagem", 300.00m, new DateOnly(2026, 10, 1), 3);

    /// <summary>
    /// A criação produz Draft, versão inicial e histórico de criação sem estado anterior.
    /// </summary>
    [TestMethod]
    public void CreateDraftProducesDraftAndCreationHistory()
    {
        (Expense expense, ExpenseHistory history) = Expense.CreateDraft("owner-1", _data, _now);

        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual("owner-1", expense.OwnerId);
        Assert.AreEqual(1, expense.Version);
        Assert.AreEqual(expense.Id, history.ExpenseId);
        Assert.AreEqual(ExpenseAction.Created, history.Action);
        Assert.IsNull(history.FromStatus);
        Assert.AreEqual(_now, history.OccurredAtUtc);
    }

    /// <summary>
    /// Transições fora de ordem lançam exceção na própria entidade, como proteção extra.
    /// </summary>
    [TestMethod]
    public void TransitionOutOfOrderThrows()
    {
        (Expense expense, _) = Expense.CreateDraft("owner-1", _data, _now);

        Assert.ThrowsExactly<InvalidOperationException>(() => expense.Approve("approver-1", _now));
        Assert.ThrowsExactly<InvalidOperationException>(() => expense.Pay("finance-1", _now));
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
    }

    /// <summary>
    /// Depois do envio, a edição deixa de ser permitida.
    /// </summary>
    [TestMethod]
    public void UpdateAfterSubmitThrows()
    {
        (Expense expense, _) = Expense.CreateDraft("owner-1", _data, _now);
        expense.Submit("owner-1", _now);

        Assert.ThrowsExactly<InvalidOperationException>(() => expense.UpdateDraft(_data, "owner-1", _now));
    }

    /// <summary>
    /// Editar com os mesmos valores não gera histórico nem muda a versão.
    /// </summary>
    [TestMethod]
    public void UpdateDraftWithSameDataReturnsNull()
    {
        (Expense expense, _) = Expense.CreateDraft("owner-1", _data, _now);

        ExpenseHistory? history = expense.UpdateDraft(_data, "owner-1", _now);

        Assert.IsNull(history);
        Assert.AreEqual(1, expense.Version);
    }

    /// <summary>
    /// O fluxo completo atualiza estado, atores, horários e versão.
    /// </summary>
    [TestMethod]
    public void FullFlowRecordsActorsAndPayment()
    {
        (Expense expense, _) = Expense.CreateDraft("owner-1", _data, _now);

        expense.Submit("owner-1", _now.AddMinutes(1));
        expense.Approve("approver-1", _now.AddMinutes(2));
        (ExpenseHistory history, PaymentRecord payment) = expense.Pay("finance-1", _now.AddMinutes(3));

        Assert.AreEqual(ExpenseStatus.Paid, expense.Status);
        Assert.AreEqual("approver-1", expense.DecidedById);
        Assert.AreEqual("finance-1", expense.PaidById);
        Assert.AreEqual(4, expense.Version);
        Assert.AreEqual(ExpenseStatus.Approved, history.FromStatus);
        Assert.AreEqual(ExpenseStatus.Paid, history.ToStatus);
        Assert.AreEqual(expense.Id, payment.ExpenseId);
        Assert.AreEqual(300.00m, payment.Amount);
        Assert.AreEqual(_now.AddMinutes(3), payment.PaidAtUtc);
    }

    /// <summary>
    /// A reprovação guarda a justificativa no reembolso e no histórico.
    /// </summary>
    [TestMethod]
    public void RejectStoresJustification()
    {
        (Expense expense, _) = Expense.CreateDraft("owner-1", _data, _now);
        expense.Submit("owner-1", _now);

        ExpenseHistory history = expense.Reject("approver-1", "Comprovante não anexado ao pedido.", _now);

        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);
        Assert.AreEqual("Comprovante não anexado ao pedido.", expense.RejectionReason);
        Assert.AreEqual("Comprovante não anexado ao pedido.", history.Justification);
    }
}

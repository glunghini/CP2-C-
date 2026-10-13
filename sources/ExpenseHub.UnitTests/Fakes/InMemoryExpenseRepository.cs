using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application.Expenses;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Repositório em memória que simula a unidade de trabalho: inclusões ficam pendentes
/// até <see cref="TrySaveChangesAsync"/> e são descartadas quando a gravação falha.
/// </summary>
internal sealed class InMemoryExpenseRepository : IExpenseRepository
{
    private readonly List<Expense> _expenses = [];
    private readonly List<ExpenseHistory> _history = [];
    private readonly List<PaymentRecord> _payments = [];
    private readonly List<Expense> _pendingExpenses = [];
    private readonly List<ExpenseHistory> _pendingHistory = [];
    private readonly List<PaymentRecord> _pendingPayments = [];
    private readonly List<ExpenseCategory> _categories =
    [
        new ExpenseCategory { Id = 1, Name = "Alimentação" },
        new ExpenseCategory { Id = 2, Name = "Transporte" },
    ];

    /// <summary>
    /// Reembolsos gravados.
    /// </summary>
    public IReadOnlyList<Expense> Expenses => _expenses;

    /// <summary>
    /// Histórico gravado.
    /// </summary>
    public IReadOnlyList<ExpenseHistory> History => _history;

    /// <summary>
    /// Pagamentos gravados.
    /// </summary>
    public IReadOnlyList<PaymentRecord> Payments => _payments;

    /// <summary>
    /// Quantidade de gravações concluídas.
    /// </summary>
    public int SaveCount { get; private set; }

    /// <summary>
    /// Quando verdadeiro, a próxima gravação simula conflito de concorrência.
    /// </summary>
    public bool FailNextSave { get; set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken)
    {
        Func<Expense, bool> predicate = filter.Compile();
        IReadOnlyList<Expense> result = _expenses.Where(predicate).OrderByDescending(expense => expense.CreatedAtUtc).ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Expense?> FindAsync(Guid id, Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken)
    {
        Func<Expense, bool> predicate = filter.Compile();
        return Task.FromResult(_expenses.Where(predicate).FirstOrDefault(expense => expense.Id == id));
    }

    /// <inheritdoc />
    public Task<Expense?> FindForUpdateAsync(Guid id, Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken)
    {
        return FindAsync(id, filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_categories.Exists(category => category.Id == categoryId));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ExpenseCategory>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ExpenseCategory> result = _categories;
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ExpenseHistory>> ListHistoryAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ExpenseHistory> result = _history
            .Where(entry => entry.ExpenseId == expenseId)
            .OrderBy(entry => entry.OccurredAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public void Add(Expense expense)
    {
        _pendingExpenses.Add(expense);
    }

    /// <inheritdoc />
    public void AddHistory(ExpenseHistory history)
    {
        _pendingHistory.Add(history);
    }

    /// <inheritdoc />
    public void AddPayment(PaymentRecord payment)
    {
        _pendingPayments.Add(payment);
    }

    /// <inheritdoc />
    public Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            ClearPending();
            return Task.FromResult(false);
        }

        _expenses.AddRange(_pendingExpenses);
        _history.AddRange(_pendingHistory);
        _payments.AddRange(_pendingPayments);
        ClearPending();
        SaveCount++;
        return Task.FromResult(true);
    }

    private void ClearPending()
    {
        _pendingExpenses.Clear();
        _pendingHistory.Clear();
        _pendingPayments.Clear();
    }
}

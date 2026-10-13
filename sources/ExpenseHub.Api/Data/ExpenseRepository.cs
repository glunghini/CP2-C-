using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application.Expenses;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Implementação do repositório de reembolsos com Entity Framework Core.
/// </summary>
internal sealed class ExpenseRepository : IExpenseRepository
{
    private readonly ExpenseHubDbContext _dbContext;

    /// <summary>
    /// Inicializa o repositório.
    /// </summary>
    /// <param name="dbContext">Contexto do banco.</param>
    public ExpenseRepository(ExpenseHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Expense>> ListAsync(
        Expression<Func<Expense, bool>> filter,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Expenses
            .AsNoTracking()
            .Where(filter)
            .OrderByDescending(expense => expense.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Expense?> FindAsync(
        Guid id,
        Expression<Func<Expense, bool>> filter,
        CancellationToken cancellationToken)
    {
        return _dbContext.Expenses
            .AsNoTracking()
            .Where(filter)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Expense?> FindForUpdateAsync(
        Guid id,
        Expression<Func<Expense, bool>> filter,
        CancellationToken cancellationToken)
    {
        return _dbContext.Expenses
            .AsTracking()
            .Where(filter)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        return _dbContext.ExpenseCategories
            .AsNoTracking()
            .AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExpenseCategory>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ExpenseCategories
            .AsNoTracking()
            .OrderBy(category => category.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExpenseHistory>> ListHistoryAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        return await _dbContext.ExpenseHistories
            .AsNoTracking()
            .Where(history => history.ExpenseId == expenseId)
            .OrderBy(history => history.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Expense expense)
    {
        _dbContext.Expenses.Add(expense);
    }

    /// <inheritdoc />
    public void AddHistory(ExpenseHistory history)
    {
        _dbContext.ExpenseHistories.Add(history);
    }

    /// <inheritdoc />
    public void AddPayment(PaymentRecord payment)
    {
        _dbContext.PaymentRecords.Add(payment);
    }

    /// <inheritdoc />
    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}

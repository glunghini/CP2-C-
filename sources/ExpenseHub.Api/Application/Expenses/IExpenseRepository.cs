using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Application.Expenses;

/// <summary>
/// Acesso a dados dos reembolsos. Filtros são aplicados no banco, antes da materialização.
/// </summary>
internal interface IExpenseRepository
{
    /// <summary>
    /// Lista os reembolsos que satisfazem o filtro, do mais recente para o mais antigo.
    /// </summary>
    /// <param name="filter">Filtro de visibilidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos encontrados.</returns>
    Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken);

    /// <summary>
    /// Busca um reembolso dentro do filtro informado.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="filter">Filtro de visibilidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso ou <c>null</c>.</returns>
    Task<Expense?> FindAsync(Guid id, Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken);

    /// <summary>
    /// Busca um reembolso dentro do filtro informado, rastreado para alteração.
    /// </summary>
    /// <param name="id">Identificador.</param>
    /// <param name="filter">Escopo permitido para a operação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso ou <c>null</c>.</returns>
    Task<Expense?> FindForUpdateAsync(Guid id, Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se a categoria existe.
    /// </summary>
    /// <param name="categoryId">Categoria.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> quando existe.</returns>
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista as categorias disponíveis.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>As categorias ordenadas por identificador.</returns>
    Task<IReadOnlyList<ExpenseCategory>> ListCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lista o histórico de um reembolso em ordem cronológica.
    /// </summary>
    /// <param name="expenseId">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os registros de histórico.</returns>
    Task<IReadOnlyList<ExpenseHistory>> ListHistoryAsync(Guid expenseId, CancellationToken cancellationToken);

    /// <summary>
    /// Marca um novo reembolso para inclusão.
    /// </summary>
    /// <param name="expense">Reembolso.</param>
    void Add(Expense expense);

    /// <summary>
    /// Marca um novo registro de histórico para inclusão.
    /// </summary>
    /// <param name="history">Registro.</param>
    void AddHistory(ExpenseHistory history);

    /// <summary>
    /// Marca um novo pagamento para inclusão.
    /// </summary>
    /// <param name="payment">Pagamento.</param>
    void AddPayment(PaymentRecord payment);

    /// <summary>
    /// Persiste todas as alterações pendentes em uma única transação.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>false</c> quando outra requisição alterou o reembolso ao mesmo tempo.</returns>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}

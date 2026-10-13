using ExpenseHub.Api.Data.Configurations;
using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Contexto do Entity Framework Core com as tabelas do Identity e do domínio de reembolsos.
/// </summary>
internal sealed class ExpenseHubDbContext : IdentityDbContext<IdentityUser>
{
    /// <summary>
    /// Inicializa o contexto com as opções configuradas na injeção de dependência.
    /// </summary>
    /// <param name="options">Opções do contexto.</param>
    public ExpenseHubDbContext(DbContextOptions<ExpenseHubDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Reembolsos.
    /// </summary>
    public DbSet<Expense> Expenses => Set<Expense>();

    /// <summary>
    /// Categorias de despesa.
    /// </summary>
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    /// <summary>
    /// Histórico de ações dos reembolsos.
    /// </summary>
    public DbSet<ExpenseHistory> ExpenseHistories => Set<ExpenseHistory>();

    /// <summary>
    /// Pagamentos registrados.
    /// </summary>
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ExpenseCategoryConfiguration());
        builder.ApplyConfiguration(new ExpenseConfiguration());
        builder.ApplyConfiguration(new ExpenseHistoryConfiguration());
        builder.ApplyConfiguration(new PaymentRecordConfiguration());
    }
}

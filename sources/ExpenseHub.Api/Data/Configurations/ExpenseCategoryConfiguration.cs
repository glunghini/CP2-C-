using System;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento da tabela de categorias e categorias iniciais.
/// </summary>
internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ExpenseCategories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(category => category.Name).IsUnique();

        builder.HasData(
            new ExpenseCategory { Id = 1, Name = "Alimentação" },
            new ExpenseCategory { Id = 2, Name = "Transporte" },
            new ExpenseCategory { Id = 3, Name = "Hospedagem" },
            new ExpenseCategory { Id = 4, Name = "Material de escritório" },
            new ExpenseCategory { Id = 5, Name = "Outros" });
    }
}

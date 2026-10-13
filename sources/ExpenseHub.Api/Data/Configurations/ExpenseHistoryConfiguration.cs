using System;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseHub.Api.Data.Configurations;

/// <summary>
/// Mapeamento relacional de <c>ExpenseHistory</c>.
/// </summary>
internal sealed class ExpenseHistoryConfiguration : IEntityTypeConfiguration<ExpenseHistory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpenseHistory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ExpenseHistories");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();
        builder.Property(history => history.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.ActorId).IsRequired().HasMaxLength(450);
        builder.Property(history => history.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.ToStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.Justification).HasMaxLength(500);

        builder.HasOne<Expense>()
            .WithMany()
            .HasForeignKey(history => history.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => new { history.ExpenseId, history.OccurredAtUtc });
    }
}

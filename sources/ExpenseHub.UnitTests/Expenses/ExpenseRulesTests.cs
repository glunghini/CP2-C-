using System;
using System.Globalization;
using ExpenseHub.Api.Application.Expenses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Expenses;

/// <summary>
/// Testes das validações de domínio de descrição, valor, data e justificativa.
/// </summary>
[TestClass]
public sealed class ExpenseRulesTests
{
    private static readonly DateOnly _today = new(2026, 10, 13);

    /// <summary>
    /// Os limites de tamanho da descrição são inclusivos.
    /// </summary>
    /// <param name="length">Tamanho da descrição.</param>
    /// <param name="valid">Resultado esperado.</param>
    [TestMethod]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(500, true)]
    [DataRow(501, false)]
    public void DescriptionLengthLimits(int length, bool valid)
    {
        string? error = ExpenseRules.ValidateDraft(new string('a', length), 10m, _today, _today);

        Assert.AreEqual(valid, error is null);
    }

    /// <summary>
    /// O valor deve estar entre R$ 0,01 e Int32.MaxValue.
    /// </summary>
    /// <param name="amount">Valor em texto invariante.</param>
    /// <param name="valid">Resultado esperado.</param>
    [TestMethod]
    [DataRow("0", false)]
    [DataRow("-10", false)]
    [DataRow("0.009", false)]
    [DataRow("0.01", true)]
    [DataRow("2147483647", true)]
    [DataRow("2147483647.01", false)]
    public void AmountLimits(string amount, bool valid)
    {
        decimal value = decimal.Parse(amount, CultureInfo.InvariantCulture);

        string? error = ExpenseRules.ValidateDraft("Descrição válida", value, _today, _today);

        Assert.AreEqual(valid, error is null);
    }

    /// <summary>
    /// A data da despesa pode ser hoje, mas não no futuro.
    /// </summary>
    [TestMethod]
    public void ExpenseDateCannotBeInTheFuture()
    {
        Assert.IsNull(ExpenseRules.ValidateDraft("Descrição válida", 10m, _today, _today));
        Assert.IsNotNull(ExpenseRules.ValidateDraft("Descrição válida", 10m, _today.AddDays(1), _today));
    }

    /// <summary>
    /// A justificativa normalizada deve ter entre 10 e 500 caracteres.
    /// </summary>
    [TestMethod]
    public void JustificationIsNormalizedAndValidated()
    {
        Assert.AreEqual("Sem nota fiscal", ExpenseRules.Normalize("  Sem nota fiscal  "));
        Assert.AreEqual(string.Empty, ExpenseRules.Normalize(null));
        Assert.IsNotNull(ExpenseRules.ValidateJustification(ExpenseRules.Normalize("   curta   ")));
        Assert.IsNull(ExpenseRules.ValidateJustification("Sem nota fiscal"));
        Assert.IsNotNull(ExpenseRules.ValidateJustification(new string('x', 501)));
    }
}

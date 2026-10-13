using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

/// <summary>
/// Testes da tabela de transições de estado.
/// </summary>
[TestClass]
public sealed class ExpenseStateMachineTests
{
    /// <summary>
    /// Cada transição prevista no contrato leva ao estado esperado.
    /// </summary>
    /// <param name="current">Estado atual.</param>
    /// <param name="action">Ação.</param>
    /// <param name="expected">Próximo estado.</param>
    [TestMethod]
    [DataRow((int)ExpenseStatus.Draft, (int)ExpenseAction.Updated, (int)ExpenseStatus.Draft)]
    [DataRow((int)ExpenseStatus.Draft, (int)ExpenseAction.Submitted, (int)ExpenseStatus.Submitted)]
    [DataRow((int)ExpenseStatus.Submitted, (int)ExpenseAction.Approved, (int)ExpenseStatus.Approved)]
    [DataRow((int)ExpenseStatus.Submitted, (int)ExpenseAction.Rejected, (int)ExpenseStatus.Rejected)]
    [DataRow((int)ExpenseStatus.Approved, (int)ExpenseAction.Paid, (int)ExpenseStatus.Paid)]
    public void AllowedTransitionReturnsNextStatus(int current, int action, int expected)
    {
        bool allowed = ExpenseStateMachine.TryGetNextStatus((ExpenseStatus)current, (ExpenseAction)action, out ExpenseStatus next);

        Assert.IsTrue(allowed);
        Assert.AreEqual((ExpenseStatus)expected, next);
    }

    /// <summary>
    /// Transições fora do contrato são recusadas, incluindo repetições e ações sobre estados finais.
    /// </summary>
    /// <param name="current">Estado atual.</param>
    /// <param name="action">Ação.</param>
    [TestMethod]
    [DataRow((int)ExpenseStatus.Draft, (int)ExpenseAction.Approved)]
    [DataRow((int)ExpenseStatus.Draft, (int)ExpenseAction.Rejected)]
    [DataRow((int)ExpenseStatus.Draft, (int)ExpenseAction.Paid)]
    [DataRow((int)ExpenseStatus.Submitted, (int)ExpenseAction.Updated)]
    [DataRow((int)ExpenseStatus.Submitted, (int)ExpenseAction.Submitted)]
    [DataRow((int)ExpenseStatus.Submitted, (int)ExpenseAction.Paid)]
    [DataRow((int)ExpenseStatus.Approved, (int)ExpenseAction.Approved)]
    [DataRow((int)ExpenseStatus.Approved, (int)ExpenseAction.Rejected)]
    [DataRow((int)ExpenseStatus.Approved, (int)ExpenseAction.Updated)]
    [DataRow((int)ExpenseStatus.Rejected, (int)ExpenseAction.Submitted)]
    [DataRow((int)ExpenseStatus.Rejected, (int)ExpenseAction.Approved)]
    [DataRow((int)ExpenseStatus.Rejected, (int)ExpenseAction.Paid)]
    [DataRow((int)ExpenseStatus.Paid, (int)ExpenseAction.Paid)]
    [DataRow((int)ExpenseStatus.Paid, (int)ExpenseAction.Updated)]
    [DataRow((int)ExpenseStatus.Draft, (int)ExpenseAction.Created)]
    public void ForbiddenTransitionIsRejected(int current, int action)
    {
        bool allowed = ExpenseStateMachine.CanApply((ExpenseStatus)current, (ExpenseAction)action);

        Assert.IsFalse(allowed);
    }

    /// <summary>
    /// Somente Rejected e Paid são finais.
    /// </summary>
    [TestMethod]
    public void OnlyRejectedAndPaidAreFinal()
    {
        Assert.IsFalse(ExpenseStateMachine.IsFinal(ExpenseStatus.Draft));
        Assert.IsFalse(ExpenseStateMachine.IsFinal(ExpenseStatus.Submitted));
        Assert.IsFalse(ExpenseStateMachine.IsFinal(ExpenseStatus.Approved));
        Assert.IsTrue(ExpenseStateMachine.IsFinal(ExpenseStatus.Rejected));
        Assert.IsTrue(ExpenseStateMachine.IsFinal(ExpenseStatus.Paid));
    }
}

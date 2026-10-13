using System;
using ExpenseHub.Api.Application.Expenses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Expenses;

/// <summary>
/// Testes dos filtros de leitura e de fluxo por perfil.
/// </summary>
[TestClass]
public sealed class ExpenseVisibilityTests
{
    private static readonly DateTime _now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ExpenseDraftData _data = new("Passagem de ônibus", 45.00m, new DateOnly(2026, 10, 2), 2);

    /// <summary>
    /// Employee enxerga somente os próprios reembolsos, em qualquer estado.
    /// </summary>
    [TestMethod]
    public void EmployeeSeesOnlyOwnExpenses()
    {
        Expense ownDraft = CreateDraft(TestUsers.Ana.UserId);
        Expense otherSubmitted = CreateSubmitted(TestUsers.Bruno.UserId);

        Assert.IsTrue(CanRead(TestUsers.Ana, ownDraft));
        Assert.IsFalse(CanRead(TestUsers.Ana, otherSubmitted));
    }

    /// <summary>
    /// Approver enxerga somente Submitted.
    /// </summary>
    [TestMethod]
    public void ApproverSeesOnlySubmitted()
    {
        Assert.IsFalse(CanRead(TestUsers.Approver, CreateDraft(TestUsers.Ana.UserId)));
        Assert.IsTrue(CanRead(TestUsers.Approver, CreateSubmitted(TestUsers.Ana.UserId)));
        Assert.IsFalse(CanRead(TestUsers.Approver, CreateApproved(TestUsers.Ana.UserId)));
    }

    /// <summary>
    /// Finance enxerga somente Approved e Paid.
    /// </summary>
    [TestMethod]
    public void FinanceSeesOnlyApprovedAndPaid()
    {
        Expense paid = CreateApproved(TestUsers.Ana.UserId);
        paid.Pay(TestUsers.Finance.UserId, _now);

        Assert.IsFalse(CanRead(TestUsers.Finance, CreateSubmitted(TestUsers.Ana.UserId)));
        Assert.IsTrue(CanRead(TestUsers.Finance, CreateApproved(TestUsers.Ana.UserId)));
        Assert.IsTrue(CanRead(TestUsers.Finance, paid));
    }

    /// <summary>
    /// Auditor enxerga todos, inclusive rascunhos.
    /// </summary>
    [TestMethod]
    public void AuditorSeesEverything()
    {
        Assert.IsTrue(CanRead(TestUsers.Auditor, CreateDraft(TestUsers.Ana.UserId)));
        Assert.IsTrue(CanRead(TestUsers.Auditor, CreateApproved(TestUsers.Bruno.UserId)));
    }

    /// <summary>
    /// Admin e usuários sem role não recebem escopo de leitura.
    /// </summary>
    [TestMethod]
    public void AdminAndUserWithoutRoleSeeNothing()
    {
        Expense submitted = CreateSubmitted(TestUsers.Ana.UserId);

        Assert.IsFalse(CanRead(TestUsers.Admin, submitted));
        Assert.IsFalse(CanRead(TestUsers.NoRole, submitted));
    }

    /// <summary>
    /// Roles acumuladas recebem a união dos escopos.
    /// </summary>
    [TestMethod]
    public void AccumulatedRolesReceiveUnionOfScopes()
    {
        CurrentUser approverEmployee = TestUsers.ApproverEmployee;

        Assert.IsTrue(CanRead(approverEmployee, CreateDraft(approverEmployee.UserId)));
        Assert.IsTrue(CanRead(approverEmployee, CreateSubmitted(TestUsers.Ana.UserId)));
        Assert.IsFalse(CanRead(approverEmployee, CreateDraft(TestUsers.Ana.UserId)));
    }

    /// <summary>
    /// No escopo de fluxo, rascunhos alheios ficam ocultos e reembolsos já enviados ficam acessíveis.
    /// </summary>
    [TestMethod]
    public void WorkflowScopeHidesOthersDrafts()
    {
        Func<Expense, bool> scope = ExpenseVisibility.ForWorkflow(TestUsers.Approver).Compile();

        Assert.IsFalse(scope(CreateDraft(TestUsers.Ana.UserId)));
        Assert.IsTrue(scope(CreateSubmitted(TestUsers.Ana.UserId)));
        Assert.IsTrue(scope(CreateApproved(TestUsers.Ana.UserId)));
    }

    private static bool CanRead(CurrentUser user, Expense expense)
    {
        return ExpenseVisibility.For(user).Compile()(expense);
    }

    private static Expense CreateDraft(string ownerId)
    {
        (Expense expense, _) = Expense.CreateDraft(ownerId, _data, _now);
        return expense;
    }

    private static Expense CreateSubmitted(string ownerId)
    {
        Expense expense = CreateDraft(ownerId);
        expense.Submit(ownerId, _now);
        return expense;
    }

    private static Expense CreateApproved(string ownerId)
    {
        Expense expense = CreateSubmitted(ownerId);
        expense.Approve(TestUsers.Approver.UserId, _now);
        return expense;
    }
}

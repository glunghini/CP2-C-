using System.Collections.Generic;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Application.Admin;
using ExpenseHub.Api.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Security;

/// <summary>
/// Testes das regras de alteração de roles pelo Admin.
/// </summary>
[TestClass]
public sealed class RoleChangeRulesTests
{
    /// <summary>
    /// Roles conhecidas são normalizadas, sem repetição e em ordem.
    /// </summary>
    [TestMethod]
    public void KnownRolesAreNormalized()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize(["approver", " Employee ", "EMPLOYEE"], false);

        Assert.IsNotNull(result.Value);
        Assert.HasCount(2, result.Value);
        Assert.AreEqual(AppRoles.Approver, result.Value[0]);
        Assert.AreEqual(AppRoles.Employee, result.Value[1]);
    }

    /// <summary>
    /// Roles desconhecidas são recusadas.
    /// </summary>
    [TestMethod]
    public void UnknownRoleIsRejected()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize([AppRoles.Employee, "SuperUser"], false);

        Assert.IsNotNull(result.Error);
        Assert.AreEqual(ServiceErrorKind.Validation, result.Error.Kind);
    }

    /// <summary>
    /// O Admin não remove a própria role Admin.
    /// </summary>
    [TestMethod]
    public void AdminCannotRemoveOwnAdminRole()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize([AppRoles.Employee], true);

        Assert.IsNotNull(result.Error);
        Assert.AreEqual(ServiceErrorKind.Conflict, result.Error.Kind);
    }

    /// <summary>
    /// O Admin pode acumular roles mantendo Admin.
    /// </summary>
    [TestMethod]
    public void AdminCanAddRolesWhileKeepingAdmin()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize([AppRoles.Admin, AppRoles.Auditor], true);

        Assert.IsNull(result.Error);
    }

    /// <summary>
    /// Uma lista vazia remove todas as roles de outro usuário.
    /// </summary>
    [TestMethod]
    public void EmptyListRemovesAllRolesFromAnotherUser()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize([], false);

        Assert.IsNotNull(result.Value);
        Assert.IsEmpty(result.Value);
    }
}

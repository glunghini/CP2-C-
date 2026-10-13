using System.Security.Claims;
using ExpenseHub.Api.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Security;

/// <summary>
/// Testes da construção da identidade a partir das claims do token.
/// </summary>
[TestClass]
public sealed class CurrentUserTests
{
    /// <summary>
    /// Identificador e roles vêm das claims do token.
    /// </summary>
    [TestMethod]
    public void TryCreateReadsSubjectAndRoles()
    {
        Claim[] claims =
        [
            new Claim(AppClaimTypes.Subject, "user-1"),
            new Claim(AppClaimTypes.Role, AppRoles.Employee),
            new Claim(AppClaimTypes.Role, AppRoles.Approver),
        ];
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "Bearer"));

        bool created = CurrentUser.TryCreate(principal, out CurrentUser? user);

        Assert.IsTrue(created);
        Assert.IsNotNull(user);
        Assert.AreEqual("user-1", user.UserId);
        Assert.IsTrue(user.IsInRole(AppRoles.Employee));
        Assert.IsTrue(user.IsInRole(AppRoles.Approver));
        Assert.IsFalse(user.IsInRole(AppRoles.Admin));
        Assert.IsTrue(user.Owns("user-1"));
        Assert.IsFalse(user.Owns("user-2"));
    }

    /// <summary>
    /// Sem identificador no token, a identidade não é criada.
    /// </summary>
    [TestMethod]
    public void TryCreateFailsWithoutSubject()
    {
        Claim[] claims = [new Claim(AppClaimTypes.Role, AppRoles.Admin)];
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "Bearer"));

        bool created = CurrentUser.TryCreate(principal, out CurrentUser? user);

        Assert.IsFalse(created);
        Assert.IsNull(user);
    }

    /// <summary>
    /// Roles não reconhecem diferença de grafia, evitando elevação por variação de caixa.
    /// </summary>
    [TestMethod]
    public void RoleCheckIsCaseSensitive()
    {
        CurrentUser user = new("user-1", ["employee"]);

        Assert.IsFalse(user.IsInRole(AppRoles.Employee));
    }
}

using ExpenseHub.Api.Security;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Usuários prontos para os cenários de autorização.
/// </summary>
internal static class TestUsers
{
    public static CurrentUser Ana { get; } = new("employee-ana", [AppRoles.Employee]);

    public static CurrentUser Bruno { get; } = new("employee-bruno", [AppRoles.Employee]);

    public static CurrentUser Approver { get; } = new("approver-carla", [AppRoles.Approver]);

    public static CurrentUser ApproverEmployee { get; } = new("approver-davi", [AppRoles.Approver, AppRoles.Employee]);

    public static CurrentUser Finance { get; } = new("finance-elisa", [AppRoles.Finance]);

    public static CurrentUser FinanceEmployee { get; } = new("finance-fabio", [AppRoles.Finance, AppRoles.Employee]);

    public static CurrentUser Auditor { get; } = new("auditor-gabi", [AppRoles.Auditor]);

    public static CurrentUser Admin { get; } = new("admin-hugo", [AppRoles.Admin]);

    public static CurrentUser NoRole { get; } = new("user-sem-role", []);
}

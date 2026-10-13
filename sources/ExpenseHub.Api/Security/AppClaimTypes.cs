namespace ExpenseHub.Api.Security;

/// <summary>
/// Tipos de claim emitidos no token de acesso.
/// </summary>
internal static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string SecurityStamp = "stamp";
}

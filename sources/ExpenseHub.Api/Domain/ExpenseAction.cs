namespace ExpenseHub.Api.Domain;

/// <summary>
/// Ações registradas no ciclo de vida de um reembolso.
/// </summary>
internal enum ExpenseAction
{
    Created = 0,
    Updated = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    Paid = 5,
}

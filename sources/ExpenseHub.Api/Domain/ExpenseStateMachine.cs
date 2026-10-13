namespace ExpenseHub.Api.Domain;

/// <summary>
/// Tabela de transições permitidas entre os estados de um reembolso.
/// </summary>
internal static class ExpenseStateMachine
{
    /// <summary>
    /// Tenta obter o próximo estado para uma ação aplicada ao estado atual.
    /// </summary>
    /// <param name="current">Estado atual.</param>
    /// <param name="action">Ação desejada.</param>
    /// <param name="next">Próximo estado quando a transição é permitida.</param>
    /// <returns><c>true</c> quando a transição é permitida.</returns>
    public static bool TryGetNextStatus(ExpenseStatus current, ExpenseAction action, out ExpenseStatus next)
    {
        ExpenseStatus? target = (current, action) switch
        {
            (ExpenseStatus.Draft, ExpenseAction.Updated) => ExpenseStatus.Draft,
            (ExpenseStatus.Draft, ExpenseAction.Submitted) => ExpenseStatus.Submitted,
            (ExpenseStatus.Submitted, ExpenseAction.Approved) => ExpenseStatus.Approved,
            (ExpenseStatus.Submitted, ExpenseAction.Rejected) => ExpenseStatus.Rejected,
            (ExpenseStatus.Approved, ExpenseAction.Paid) => ExpenseStatus.Paid,
            _ => null,
        };

        next = target ?? current;
        return target.HasValue;
    }

    /// <summary>
    /// Indica se a ação pode ser aplicada ao estado atual.
    /// </summary>
    /// <param name="current">Estado atual.</param>
    /// <param name="action">Ação desejada.</param>
    /// <returns><c>true</c> quando a transição é permitida.</returns>
    public static bool CanApply(ExpenseStatus current, ExpenseAction action)
    {
        return TryGetNextStatus(current, action, out _);
    }

    /// <summary>
    /// Indica se o estado é final.
    /// </summary>
    /// <param name="status">Estado avaliado.</param>
    /// <returns><c>true</c> para <c>Rejected</c> e <c>Paid</c>.</returns>
    public static bool IsFinal(ExpenseStatus status)
    {
        return status is ExpenseStatus.Rejected or ExpenseStatus.Paid;
    }
}

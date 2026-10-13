namespace ExpenseHub.Api.Domain;

/// <summary>
/// Categoria de despesa usada para classificar um reembolso.
/// </summary>
internal sealed class ExpenseCategory
{
    /// <summary>
    /// Identificador da categoria.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome exibido da categoria.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

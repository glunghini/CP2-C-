using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados aceitos na criação e na edição de um rascunho.
/// Proprietário, estado, atores e horários não fazem parte do contrato e são rejeitados.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class ExpenseDraftRequest
{
    /// <summary>
    /// Descrição da despesa.
    /// </summary>
    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "A descrição deve ter entre 10 e 500 caracteres.")]
    public string? Description { get; set; }

    /// <summary>
    /// Valor do reembolso.
    /// </summary>
    [Required(ErrorMessage = "O valor é obrigatório.")]
    [Range(0.01, 2147483647.0, ErrorMessage = "O valor deve estar entre R$ 0,01 e R$ 2.147.483.647,00.")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Data em que a despesa ocorreu.
    /// </summary>
    [Required(ErrorMessage = "A data da despesa é obrigatória.")]
    public DateOnly? ExpenseDate { get; set; }

    /// <summary>
    /// Categoria da despesa.
    /// </summary>
    [Required(ErrorMessage = "A categoria é obrigatória.")]
    [Range(1, int.MaxValue, ErrorMessage = "Informe uma categoria válida.")]
    public int? CategoryId { get; set; }
}

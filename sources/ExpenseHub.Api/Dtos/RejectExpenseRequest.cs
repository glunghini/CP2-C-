using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados da reprovação de um reembolso.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class RejectExpenseRequest
{
    /// <summary>
    /// Justificativa obrigatória da reprovação.
    /// </summary>
    [Required(ErrorMessage = "A justificativa é obrigatória.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "A justificativa deve ter entre 10 e 500 caracteres.")]
    public string? Justification { get; set; }
}

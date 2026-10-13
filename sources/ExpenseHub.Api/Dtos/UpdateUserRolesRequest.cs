using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Conjunto completo de roles que o usuário deve possuir após a alteração.
/// Uma lista vazia remove todas as roles.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class UpdateUserRolesRequest
{
    /// <summary>
    /// Roles desejadas.
    /// </summary>
    [Required(ErrorMessage = "Informe a lista de roles, mesmo que vazia.")]
    [MaxLength(5, ErrorMessage = "Informe no máximo 5 roles.")]
    public IReadOnlyList<string>? Roles { get; set; }
}

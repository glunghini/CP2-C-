using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Conversão de erros do Identity em mensagens.
/// </summary>
internal static class IdentityErrors
{
    /// <summary>
    /// Junta as descrições dos erros do resultado.
    /// </summary>
    /// <param name="result">Resultado com falha.</param>
    /// <returns>Mensagem única.</returns>
    public static string Describe(IdentityResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return string.Join(' ', result.Errors.Select(error => error.Description));
    }
}

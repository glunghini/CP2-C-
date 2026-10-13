using System;
using ExpenseHub.Api.Application;
using Microsoft.AspNetCore.Http;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Converte falhas de serviço em respostas <c>ProblemDetails</c>.
/// </summary>
internal static class ProblemResults
{
    /// <summary>
    /// Cria a resposta HTTP correspondente à falha.
    /// </summary>
    /// <param name="error">Falha de regra de negócio.</param>
    /// <returns>A resposta com status coerente.</returns>
    public static IResult FromError(ServiceError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        (int statusCode, string title) = error.Kind switch
        {
            ServiceErrorKind.Validation => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            ServiceErrorKind.Unauthorized => (StatusCodes.Status401Unauthorized, "Não autenticado"),
            ServiceErrorKind.Forbidden => (StatusCodes.Status403Forbidden, "Operação não permitida"),
            ServiceErrorKind.NotFound => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ServiceErrorKind.Conflict => (StatusCodes.Status409Conflict, "Conflito de estado"),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado"),
        };

        return Results.Problem(detail: error.Message, statusCode: statusCode, title: title);
    }

    /// <summary>
    /// Resposta para requisições autenticadas cujo token não identifica o usuário.
    /// </summary>
    /// <returns>Resposta 401.</returns>
    public static IResult MissingIdentity()
    {
        return Results.Problem(
            detail: "O token não identifica o usuário.",
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Não autenticado");
    }
}

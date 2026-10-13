using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Executa as validações declarativas dos DTOs antes de chamar o serviço.
/// </summary>
internal static class RequestValidation
{
    /// <summary>
    /// Valida o objeto e, em caso de erro, produz uma resposta 400 com os campos inválidos.
    /// </summary>
    /// <param name="request">DTO recebido.</param>
    /// <param name="problem">Resposta de erro quando a validação falha.</param>
    /// <returns><c>true</c> quando o DTO é válido.</returns>
    public static bool TryValidate(object request, [NotNullWhen(false)] out IResult? problem)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<ValidationResult> results = [];
        ValidationContext context = new(request);
        if (Validator.TryValidateObject(request, context, results, validateAllProperties: true))
        {
            problem = null;
            return true;
        }

        Dictionary<string, string[]> errors = results
            .SelectMany(
                result => result.MemberNames.DefaultIfEmpty(string.Empty),
                (result, member) => new { Member = member, Message = result.ErrorMessage ?? "Valor inválido." })
            .GroupBy(item => item.Member, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Message).ToArray(), StringComparer.Ordinal);

        problem = Results.ValidationProblem(errors, title: "Requisição inválida");
        return false;
    }
}

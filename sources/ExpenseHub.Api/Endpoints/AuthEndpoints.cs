using System;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Application.Auth;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Rotas públicas de cadastro e autenticação.
/// </summary>
internal static class AuthEndpoints
{
    /// <summary>
    /// Mapeia <c>POST /register</c> e <c>POST /login</c>.
    /// </summary>
    /// <param name="endpoints">Construtor de rotas.</param>
    /// <returns>O mesmo construtor, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("Register")
            .WithTags("Auth");

        endpoints.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithTags("Auth");

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, AuthService authService)
    {
        if (!RequestValidation.TryValidate(request, out IResult? problem))
        {
            return problem;
        }

        ServiceResult<UserResponse> result = await authService.RegisterAsync(request);
        return result.Match(
            user => Results.Json(user, statusCode: StatusCodes.Status201Created),
            ProblemResults.FromError);
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, AuthService authService)
    {
        if (!RequestValidation.TryValidate(request, out IResult? problem))
        {
            return problem;
        }

        ServiceResult<LoginResponse> result = await authService.LoginAsync(request);
        return result.Match(response => Results.Ok(response), ProblemResults.FromError);
    }
}

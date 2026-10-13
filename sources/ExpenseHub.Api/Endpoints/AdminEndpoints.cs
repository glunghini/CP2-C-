using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Application.Admin;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Rotas administrativas, restritas à role Admin.
/// </summary>
internal static class AdminEndpoints
{
    /// <summary>
    /// Mapeia as rotas de usuários e roles.
    /// </summary>
    /// <param name="endpoints">Construtor de rotas.</param>
    /// <returns>O mesmo construtor, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/api/admin/users")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithTags("Admin");

        group.MapGet(string.Empty, ListUsersAsync).WithName("ListUsers");
        group.MapPut("/{id}/roles", UpdateRolesAsync).WithName("UpdateUserRoles");

        return endpoints;
    }

    private static async Task<IResult> ListUsersAsync(AdminUserService adminUserService, CancellationToken cancellationToken)
    {
        IReadOnlyList<UserResponse> users = await adminUserService.ListUsersAsync(cancellationToken);
        return Results.Ok(users);
    }

    private static async Task<IResult> UpdateRolesAsync(
        string id,
        UpdateUserRolesRequest request,
        ClaimsPrincipal principal,
        AdminUserService adminUserService)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? currentUser))
        {
            return ProblemResults.MissingIdentity();
        }

        if (!RequestValidation.TryValidate(request, out IResult? problem))
        {
            return problem;
        }

        ServiceResult<UserResponse> result = await adminUserService.UpdateRolesAsync(id, currentUser.UserId, request.Roles ?? []);
        return result.Match(user => Results.Ok(user), ProblemResults.FromError);
    }
}

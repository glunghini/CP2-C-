using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Application.Admin;

/// <summary>
/// Consulta de usuários e administração de roles.
/// </summary>
internal sealed class AdminUserService
{
    private readonly ExpenseHubDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;

    /// <summary>
    /// Inicializa o serviço.
    /// </summary>
    /// <param name="dbContext">Contexto do banco.</param>
    /// <param name="userManager">Gerenciador de usuários do Identity.</param>
    public AdminUserService(ExpenseHubDbContext dbContext, UserManager<IdentityUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    /// <summary>
    /// Lista os usuários com suas roles, usando duas consultas no banco.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os usuários ordenados por e-mail.</returns>
    public async Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(cancellationToken);

        var roleLinks = await _dbContext.UserRoles
            .AsNoTracking()
            .Join(
                _dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, RoleName = role.Name })
            .ToListAsync(cancellationToken);

        ILookup<string, string> rolesByUser = roleLinks
            .Where(link => link.RoleName != null)
            .ToLookup(link => link.UserId, link => link.RoleName!, StringComparer.Ordinal);

        return users
            .Select(user => new UserResponse(user.Id, user.Email, rolesByUser[user.Id].Order(StringComparer.Ordinal).ToList()))
            .ToList();
    }

    /// <summary>
    /// Substitui as roles de um usuário e invalida os tokens já emitidos para ele.
    /// </summary>
    /// <param name="userId">Usuário alterado.</param>
    /// <param name="currentAdminId">Admin autenticado que faz a alteração.</param>
    /// <param name="requestedRoles">Roles desejadas.</param>
    /// <returns>O usuário com as roles atualizadas ou a falha.</returns>
    public async Task<ServiceResult<UserResponse>> UpdateRolesAsync(
        string userId,
        string currentAdminId,
        IReadOnlyList<string> requestedRoles)
    {
        IdentityUser? user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<UserResponse>.Failure(ServiceErrorKind.NotFound, "Usuário não encontrado.");
        }

        bool targetIsCurrentUser = string.Equals(user.Id, currentAdminId, StringComparison.Ordinal);
        ServiceResult<IReadOnlyList<string>> validation = RoleChangeRules.Normalize(requestedRoles, targetIsCurrentUser);
        if (validation.Error is not null)
        {
            return ServiceResult<UserResponse>.Failure(validation.Error);
        }

        IReadOnlyList<string> targetRoles = validation.Value ?? [];
        IList<string> currentRoles = await _userManager.GetRolesAsync(user);
        string[] rolesToRemove = currentRoles.Except(targetRoles, StringComparer.Ordinal).ToArray();
        string[] rolesToAdd = targetRoles.Except(currentRoles, StringComparer.Ordinal).ToArray();

        if (rolesToRemove.Length > 0)
        {
            IdentityResult removed = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removed.Succeeded)
            {
                return ServiceResult<UserResponse>.Failure(ServiceErrorKind.Validation, IdentityErrors.Describe(removed));
            }
        }

        if (rolesToAdd.Length > 0)
        {
            IdentityResult added = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!added.Succeeded)
            {
                return ServiceResult<UserResponse>.Failure(ServiceErrorKind.Validation, IdentityErrors.Describe(added));
            }
        }

        if (rolesToRemove.Length > 0 || rolesToAdd.Length > 0)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return ServiceResult<UserResponse>.Success(new UserResponse(user.Id, user.Email, targetRoles));
    }
}

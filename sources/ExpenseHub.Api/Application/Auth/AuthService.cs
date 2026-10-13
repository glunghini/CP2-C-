using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Application.Auth;

/// <summary>
/// Cadastro e autenticação de usuários.
/// </summary>
internal sealed class AuthService
{
    internal const string InvalidCredentialsMessage = "E-mail ou senha inválidos.";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly TokenService _tokenService;

    /// <summary>
    /// Inicializa o serviço.
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity.</param>
    /// <param name="tokenService">Emissor de tokens.</param>
    public AuthService(UserManager<IdentityUser> userManager, TokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Cadastra um usuário sem nenhuma role. Roles são atribuídas apenas pelo Admin.
    /// </summary>
    /// <param name="request">Dados de cadastro validados.</param>
    /// <returns>O usuário criado ou a falha.</returns>
    public async Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string email = request.Email.Trim();
        IdentityUser? existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return ServiceResult<UserResponse>.Failure(ServiceErrorKind.Conflict, "Já existe um usuário com este e-mail.");
        }

        IdentityUser user = new()
        {
            UserName = email,
            Email = email,
        };

        IdentityResult created = await _userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return ServiceResult<UserResponse>.Failure(ServiceErrorKind.Validation, IdentityErrors.Describe(created));
        }

        return ServiceResult<UserResponse>.Success(new UserResponse(user.Id, user.Email, []));
    }

    /// <summary>
    /// Valida as credenciais e emite o token bearer.
    /// </summary>
    /// <param name="request">Credenciais validadas.</param>
    /// <returns>O token ou a falha.</returns>
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IdentityUser? user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return ServiceResult<LoginResponse>.Failure(ServiceErrorKind.Unauthorized, InvalidCredentialsMessage);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return ServiceResult<LoginResponse>.Failure(
                ServiceErrorKind.Unauthorized,
                "Usuário temporariamente bloqueado por tentativas inválidas.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return ServiceResult<LoginResponse>.Failure(ServiceErrorKind.Unauthorized, InvalidCredentialsMessage);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        IList<string> roles = await _userManager.GetRolesAsync(user);
        return ServiceResult<LoginResponse>.Success(_tokenService.CreateAccessToken(user, roles));
    }
}

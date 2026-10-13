using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using ExpenseHub.Api.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Emite tokens bearer assinados para usuários autenticados.
/// </summary>
internal sealed class TokenService
{
    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa o serviço.
    /// </summary>
    /// <param name="settings">Configurações do token.</param>
    /// <param name="timeProvider">Relógio do servidor.</param>
    public TokenService(JwtSettings settings, TimeProvider timeProvider)
    {
        _settings = settings;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Cria o token de acesso com identificador, roles e carimbo de segurança do usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="roles">Roles atuais do usuário.</param>
    /// <returns>A resposta de login.</returns>
    public LoginResponse CreateAccessToken(IdentityUser user, IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        DateTime issuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime expiresAtUtc = issuedAtUtc.AddMinutes(_settings.ExpirationMinutes);

        List<Claim> claims =
        [
            new Claim(AppClaimTypes.Subject, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)),
            new Claim(AppClaimTypes.SecurityStamp, user.SecurityStamp ?? string.Empty),
        ];

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(AppClaimTypes.Email, user.Email));
        }

        foreach (string role in roles)
        {
            claims.Add(new Claim(AppClaimTypes.Role, role));
        }

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAtUtc,
            NotBefore = issuedAtUtc,
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(_settings.SigningKey, SecurityAlgorithms.HmacSha256),
        };

        JsonWebTokenHandler handler = new();
        string jwt = handler.CreateToken(descriptor);
        return new LoginResponse(jwt, "Bearer", expiresAtUtc);
    }
}

using System;

namespace ExpenseHub.Api.Responses;

/// <summary>
/// Resposta de autenticação com o token bearer.
/// </summary>
/// <param name="AccessToken">Token de acesso assinado.</param>
/// <param name="TokenType">Tipo do token, sempre <c>Bearer</c>.</param>
/// <param name="ExpiresAtUtc">Instante de expiração em UTC.</param>
internal sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc);

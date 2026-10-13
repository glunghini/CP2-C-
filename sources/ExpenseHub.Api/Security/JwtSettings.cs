using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Configurações de emissão e validação do token bearer.
/// A chave de assinatura nunca fica no código-fonte nem no appsettings.json.
/// </summary>
internal sealed class JwtSettings
{
    public const int MinimumKeyBytes = 32;
    public const int DefaultExpirationMinutes = 60;

    private JwtSettings(string issuer, string audience, int expirationMinutes, SymmetricSecurityKey signingKey, bool usesEphemeralKey)
    {
        Issuer = issuer;
        Audience = audience;
        ExpirationMinutes = expirationMinutes;
        SigningKey = signingKey;
        UsesEphemeralKey = usesEphemeralKey;
    }

    /// <summary>
    /// Emissor esperado do token.
    /// </summary>
    public string Issuer { get; }

    /// <summary>
    /// Público esperado do token.
    /// </summary>
    public string Audience { get; }

    /// <summary>
    /// Validade do token em minutos.
    /// </summary>
    public int ExpirationMinutes { get; }

    /// <summary>
    /// Chave simétrica usada para assinar e validar os tokens.
    /// </summary>
    public SymmetricSecurityKey SigningKey { get; }

    /// <summary>
    /// Indica que nenhuma chave foi configurada e uma chave aleatória foi gerada para esta execução.
    /// </summary>
    public bool UsesEphemeralKey { get; }

    /// <summary>
    /// Lê as configurações da seção <c>Jwt</c>.
    /// </summary>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>As configurações prontas para uso.</returns>
    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection("Jwt");
        string issuer = section["Issuer"] ?? "ExpenseHub";
        string audience = section["Audience"] ?? "ExpenseHub.Clients";
        int expirationMinutes = ParseExpiration(section["ExpirationMinutes"]);
        string? configuredSigningValue = section["SigningKey"];

        if (!string.IsNullOrWhiteSpace(configuredSigningValue)
            && Encoding.UTF8.GetByteCount(configuredSigningValue) >= MinimumKeyBytes)
        {
            byte[] configuredBytes = Encoding.UTF8.GetBytes(configuredSigningValue);
            return new JwtSettings(issuer, audience, expirationMinutes, new SymmetricSecurityKey(configuredBytes), false);
        }

        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        return new JwtSettings(issuer, audience, expirationMinutes, new SymmetricSecurityKey(randomBytes), true);
    }

    private static int ParseExpiration(string? value)
    {
        bool parsed = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes);
        return parsed && minutes > 0 ? minutes : DefaultExpirationMinutes;
    }
}

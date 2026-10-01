namespace InclusiRH.Api.Services.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Vida curta do access token: se ele vazar, a janela de uso indevido é pequena.
    /// </summary>
    public int AccessTokenExpireMinutes { get; set; } = 15;

    /// <summary>
    /// Por quantos dias o refresh token pode renovar a sessão sem novo login.
    /// </summary>
    public int RefreshTokenExpireDays { get; set; } = 7;
}

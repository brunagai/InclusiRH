using System.ComponentModel.DataAnnotations;
using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [MaxLength(128)]
    public string Senha { get; set; } = string.Empty;
}

public sealed class RegistrarUsuarioRequest
{
    [Required(ErrorMessage = "Informe o nome.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [MinLength(8, ErrorMessage = "A senha deve ter ao menos 8 caracteres.")]
    [MaxLength(128)]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o cargo.")]
    [MaxLength(150)]
    public string Cargo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o departamento.")]
    [MaxLength(150)]
    public string Departamento { get; set; } = string.Empty;

    /// <summary>
    /// Colaborador, Gestor, RecursosHumanos (RH), ComiteDei ou Administrador (Admin).
    /// </summary>
    [Required(ErrorMessage = "Informe o papel.")]
    public PapelColaborador Papel { get; set; }
}

/// <summary>
/// Pedido de renovação de sessão. O access token pode estar vencido;
/// a assinatura continua sendo conferida.
/// </summary>
public sealed class RefreshTokenRequest
{
    [Required(ErrorMessage = "Informe o access token.")]
    [MaxLength(4096)]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o refresh token.")]
    [MaxLength(256)]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Par de tokens devolvido pelo login e pela renovação.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// JWT de curta duração, enviado no cabeçalho Authorization: Bearer.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset AccessTokenExpiraEm { get; init; }

    /// <summary>
    /// Valor opaco usado apenas em POST /api/auth/refresh. Cada renovação
    /// devolve um novo e invalida o anterior.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    public DateTimeOffset RefreshTokenExpiraEm { get; init; }

    public Guid ColaboradorId { get; init; }

    public string Nome { get; init; } = string.Empty;

    public PapelColaborador Papel { get; init; }
}

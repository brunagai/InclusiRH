using System.ComponentModel.DataAnnotations;
using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.DTOs.Colaboradores;

public sealed class CadastrarColaboradorRequest
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

    [Required(ErrorMessage = "Informe o papel.")]
    public PapelColaborador Papel { get; set; }
}

public sealed class ColaboradorResponse
{
    public Guid Id { get; init; }

    public string Nome { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Cargo { get; init; } = string.Empty;

    public string Departamento { get; init; } = string.Empty;

    public PapelColaborador Papel { get; init; }

    public bool Ativo { get; init; }

    public DateTimeOffset DataCadastro { get; init; }
}

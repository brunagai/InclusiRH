using System.Text.Json.Serialization;
using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.Models;

public sealed class Colaborador
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string SenhaHash { get; set; } = string.Empty;

    public string Cargo { get; set; } = string.Empty;

    public string Departamento { get; set; } = string.Empty;

    public PapelColaborador Papel { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTimeOffset DataCadastro { get; set; }

    /// <summary>
    /// Hash SHA-256 (Base64) do refresh token ativo. O valor original só existe
    /// na resposta do login ou da renovação: quem ler o banco não consegue
    /// reutilizá-lo para abrir uma sessão.
    /// </summary>
    [JsonIgnore]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Momento (UTC) a partir do qual o refresh token deixa de renovar a sessão.
    /// </summary>
    [JsonIgnore]
    public DateTime? RefreshTokenExpiryTime { get; set; }

    [JsonIgnore]
    public Autodeclaracao? Autodeclaracao { get; set; }

    [JsonIgnore]
    public ICollection<Denuncia> Denuncias { get; set; } = new List<Denuncia>();

    [JsonIgnore]
    public ICollection<ProgressoTreinamento> ProgressosTreinamento { get; set; } = new List<ProgressoTreinamento>();
}

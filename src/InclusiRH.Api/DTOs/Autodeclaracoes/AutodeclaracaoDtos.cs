using System.ComponentModel.DataAnnotations;
using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.DTOs.Autodeclaracoes;

public sealed class SalvarAutodeclaracaoRequest
{
    [Required]
    public Etnia Etnia { get; set; }

    [Required]
    public Genero Genero { get; set; }

    [Required]
    public OrientacaoSexual OrientacaoSexual { get; set; }

    [Required]
    public SituacaoPcd SituacaoPcd { get; set; }

    [MaxLength(200)]
    public string? TipoDeficiencia { get; set; }

    public bool ConsentimentoLgpd { get; set; }
}

public sealed class AutodeclaracaoResponse
{
    public Guid Id { get; init; }

    public Guid ColaboradorId { get; init; }

    public Etnia Etnia { get; init; }

    public Genero Genero { get; init; }

    public OrientacaoSexual OrientacaoSexual { get; init; }

    public SituacaoPcd SituacaoPcd { get; init; }

    public string? TipoDeficiencia { get; init; }

    public bool ConsentimentoLgpd { get; init; }

    public DateTimeOffset DataConsentimento { get; init; }

    public DateTimeOffset DataAtualizacao { get; init; }
}

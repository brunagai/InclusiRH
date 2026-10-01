using System.ComponentModel.DataAnnotations;
using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.DTOs.Autodeclaracoes;

public sealed class AutodeclaracaoDto
{
    [Required]
    public Etnia Etnia { get; set; }

    [Required]
    public Genero Genero { get; set; }

    [Required]
    public OrientacaoSexual OrientacaoSexual { get; set; }

    /// <summary>
    /// Nulo significa que a pessoa preferiu não informar sobre deficiência.
    /// </summary>
    public bool? TemDeficiencia { get; set; }

    [MaxLength(200)]
    public string? DescricaoDeficiencia { get; set; }

    [Required]
    public ContextoSocioeconomico ContextoSocioeconomico { get; set; }

    public bool ConsentimentoLgpd { get; set; }

    public DateTimeOffset? DataAtualizacao { get; set; }
}

public sealed class IndicadorPercentualDto
{
    public string Categoria { get; init; } = string.Empty;

    public int Quantidade { get; init; }

    public decimal Percentual { get; init; }
}

public sealed class GrupoIndicadorDto
{
    public string Nome { get; init; } = string.Empty;

    public int BaseCalculo { get; init; }

    public bool Liberado { get; init; }

    public string? MotivoSupressao { get; init; }

    public IReadOnlyList<IndicadorPercentualDto> Categorias { get; init; } = [];
}

public sealed class RelatorioEsgDto
{
    public DateTimeOffset GeradoEm { get; init; }

    public int TotalColaboradoresAtivos { get; init; }

    public int TotalAutodeclaracoes { get; init; }

    public decimal PercentualCobertura { get; init; }

    public bool IndicadoresLiberados { get; init; }

    public string? AvisoSupressao { get; init; }

    public decimal? PercentualPopulacaoNegra { get; init; }

    public decimal? PercentualPcd { get; init; }

    public GrupoIndicadorDto DiversidadeEtnicoRacial { get; init; } = new();

    public GrupoIndicadorDto EquidadeDeGenero { get; init; } = new();

    public GrupoIndicadorDto InclusaoPcd { get; init; } = new();

    public GrupoIndicadorDto ContextoSocioeconomico { get; init; } = new();

    public string BaseLegal { get; init; } = string.Empty;
}

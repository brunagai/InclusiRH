using System.ComponentModel.DataAnnotations;
using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.DTOs.Treinamentos;

public sealed class CriarTreinamentoDto
{
    [Required(ErrorMessage = "Informe o título.")]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a descrição.")]
    [MaxLength(2000)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    public CategoriaTreinamento Categoria { get; set; }

    [Range(0.5, 400, ErrorMessage = "A carga horária deve estar entre 0,5 e 400 horas.")]
    public decimal CargaHoraria { get; set; }

    [Required(ErrorMessage = "Informe o módulo.")]
    [MaxLength(200)]
    public string Modulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o conteúdo.")]
    [MaxLength(4000)]
    public string Conteudo { get; set; } = string.Empty;

    public bool Obrigatorio { get; set; }
}

public sealed class AtualizarProgressoDto
{
    [Required]
    public StatusConclusaoTreinamento Status { get; set; }
}

public sealed class TreinamentoResponse
{
    public Guid Id { get; init; }

    public string Titulo { get; init; } = string.Empty;

    public string Descricao { get; init; } = string.Empty;

    public CategoriaTreinamento Categoria { get; init; }

    public string CategoriaDescricao { get; init; } = string.Empty;

    public decimal CargaHoraria { get; init; }

    public string Modulo { get; init; } = string.Empty;

    public string Conteudo { get; init; } = string.Empty;

    public bool Obrigatorio { get; init; }

    public bool Ativo { get; init; }
}

public sealed class ProgressoTreinamentoResponse
{
    public Guid Id { get; init; }

    public Guid TreinamentoId { get; init; }

    public string Titulo { get; init; } = string.Empty;

    public CategoriaTreinamento Categoria { get; init; }

    public string CategoriaDescricao { get; init; } = string.Empty;

    public Guid ColaboradorId { get; init; }

    public StatusConclusaoTreinamento Status { get; init; }

    public DateTimeOffset? DataInicio { get; init; }

    public DateTimeOffset? DataConclusao { get; init; }
}

public sealed class ConclusaoDepartamentoDto
{
    public string Departamento { get; init; } = string.Empty;

    public int ColaboradoresAtivos { get; init; }

    public int ConclusoesEsperadas { get; init; }

    public int ConclusoesRealizadas { get; init; }

    public decimal TaxaConclusao { get; init; }
}

public sealed class RelatorioConclusaoDto
{
    public DateTimeOffset GeradoEm { get; init; }

    public int TreinamentosObrigatorios { get; init; }

    public decimal TaxaConclusaoGeral { get; init; }

    public string? Aviso { get; init; }

    public IReadOnlyList<ConclusaoDepartamentoDto> Departamentos { get; init; } = [];

    public string PilarEsg { get; init; } = "Social (S)";

    public string BaseLegal { get; init; } = string.Empty;
}

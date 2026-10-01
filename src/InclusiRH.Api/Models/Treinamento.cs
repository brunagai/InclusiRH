using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.Models;

/// <summary>
/// Trilha ou curso do catálogo. O letramento étnico-racial
/// (Leis 10.639/2003 e 11.645/2008) usa a categoria correspondente
/// e pode ser marcado como obrigatório para a métrica do pilar Social.
/// </summary>
public sealed class Treinamento
{
    public Guid Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public CategoriaTreinamento Categoria { get; set; }

    public decimal CargaHoraria { get; set; }

    public string Modulo { get; set; } = string.Empty;

    public string Conteudo { get; set; } = string.Empty;

    public bool Obrigatorio { get; set; }

    public bool Ativo { get; set; } = true;

    public ICollection<ProgressoTreinamento> Progressos { get; set; } = new List<ProgressoTreinamento>();
}

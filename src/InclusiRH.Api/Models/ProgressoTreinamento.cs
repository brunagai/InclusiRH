using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.Models;

/// <summary>
/// Andamento de uma pessoa em um treinamento. A conclusão registrada
/// permanece: o status não regride depois de concluído.
/// </summary>
public sealed class ProgressoTreinamento
{
    public Guid Id { get; set; }

    public Guid TreinamentoId { get; set; }

    public Guid ColaboradorId { get; set; }

    public StatusConclusaoTreinamento Status { get; set; } = StatusConclusaoTreinamento.NaoIniciado;

    public DateTimeOffset? DataInicio { get; set; }

    public DateTimeOffset? DataConclusao { get; set; }

    public Treinamento Treinamento { get; set; } = null!;

    public Colaborador Colaborador { get; set; } = null!;
}
